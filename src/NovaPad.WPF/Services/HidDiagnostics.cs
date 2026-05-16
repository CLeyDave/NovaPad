#pragma warning disable CS0612

using System.IO;
using System.Text;
using HidSharp;
using HidSharp.Reports;
using Serilog;

namespace NovaPad.WPF.Services;

public static class HidDiagnostics
{
    private static readonly string LogDir = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "log");

    private static readonly object Lock = new();

    public static async Task RunAsync()
    {
        try
        {
            // Retry: HID devices may appear after Bluetooth pairing is established
            var allDevices = new List<HidDevice>();
            for (int retry = 0; retry < 5; retry++)
            {
                allDevices = DeviceList.Local.GetHidDevices().ToList();
                var sonyCount = allDevices.Count(d => d.VendorID == 0x054C);
                Log.Information("[HidDiagnostics] Scan #{Retry}: {Total} HID devices, {Sony} Sony devices",
                    retry + 1, allDevices.Count, sonyCount);

                if (sonyCount > 0)
                    break;

                await Task.Delay(5000);
            }

            Directory.CreateDirectory(LogDir);

            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            var filePath = Path.Combine(LogDir, $"hid-diagnostics-{timestamp}.log");

            var sb = new StringBuilder();
            sb.AppendLine("=== NovaPad HID Diagnostics ===");
            sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();

            sb.AppendLine($"Total HID devices found: {allDevices.Count}");
            sb.AppendLine();

            // Group by VID:PID for summary
            var groups = allDevices
                .GroupBy(d => $"VID=0x{d.VendorID:X4} PID=0x{d.ProductID:X4}")
                .OrderBy(g => g.Key);

            foreach (var group in groups)
            {
                sb.AppendLine($"=== {group.Key} ({group.Count()} device(s)) ===");
                foreach (var device in group.OrderByDescending(d => d.MaxInputReportLength))
                {
                    var deviceInfo = DescribeDevice(device);
                    sb.AppendLine(deviceInfo);

                    var descriptorInfo = DescribeDescriptor(device);
                    foreach (var line in descriptorInfo)
                        sb.AppendLine($"  | {line}");
                }

                sb.AppendLine();
            }

            // Highlight Sony devices
            sb.AppendLine("=== SONY DEVICES (VID=0x054C) ===");
            var sonyDevices = allDevices
                .Where(d => d.VendorID == 0x054C)
                .OrderBy(d => d.ProductID)
                .ThenByDescending(d => d.MaxInputReportLength)
                .ToList();

            sb.AppendLine($"Sony devices found: {sonyDevices.Count}");
            foreach (var device in sonyDevices)
            {
                sb.AppendLine(DescribeDevice(device));
                var descriptorInfo = DescribeDescriptor(device);
                foreach (var line in descriptorInfo)
                    sb.AppendLine($"  | {line}");
                sb.AppendLine();
            }

            // === BRUTE-FORCE FEATURE REPORT TEST on Sony devices ===
            sb.AppendLine("=== FEATURE REPORT TEST on Sony Devices ===");
            foreach (var device in sonyDevices)
            {
                sb.AppendLine($"Testing device: {DescribeDevice(device)}");
                try
                {
                    // Parse feature report IDs from descriptor
                    var featureIds = new List<byte>();
                    try
                    {
                        var desc = device.GetReportDescriptor();
                        if (desc?.DeviceItems != null)
                        {
                            foreach (var item in desc.DeviceItems)
                            {
                                foreach (var r in item.FeatureReports ?? Array.Empty<Report>())
                                {
                                    if (r.ReportID != 0)
                                        featureIds.Add(r.ReportID);
                                }
                            }
                        }
                    }
                    catch { }

                    sb.AppendLine($"  Feature report IDs from descriptor: [{string.Join(", ", featureIds.Select(id => $"0x{id:X2}"))}]");

                    if (device.TryOpen(out var stream))
                    {
                        using (stream)
                        {
                            // Try to get each known feature report
                            foreach (var id in featureIds)
                            {
                                var featBuf = new byte[Math.Max(device.MaxFeatureReportLength, 64)];
                                featBuf[0] = id;
                                try
                                {
                                    stream.GetFeature(featBuf);
                                    var hex = BitConverter.ToString(featBuf, 0, Math.Min(featBuf.Length, 64));
                                    sb.AppendLine($"  GET_FEATURE ID=0x{id:X2} ({featBuf.Length} buf): {hex}");
                                }
                                catch (Exception ex)
                                {
                                    sb.AppendLine($"  GET_FEATURE ID=0x{id:X2}: FAILED - {ex.Message}");
                                }
                            }

                            // Try sending output report via SetFeature with ID 0x11
                            var outReport = new byte[78];
                            outReport[0] = 0x11;
                            outReport[1] = 0xC0;
                            outReport[4] = 255;
                            outReport[7] = 0xFF;
                            var crc = CalcCrc(outReport, 0, 74);
                            outReport[74] = (byte)(crc & 0xFF);
                            outReport[75] = (byte)((crc >> 8) & 0xFF);
                            try
                            {
                                stream.SetFeature(outReport);
                                sb.AppendLine("  SET_FEATURE ID=0x11 (78-byte output as feature): SUCCESS");
                            }
                            catch (Exception ex)
                            {
                                sb.AppendLine($"  SET_FEATURE ID=0x11 (78-byte output as feature): FAILED - {ex.Message}");
                            }

                            // Also try SetFeature with the 547-byte padded report
                            var fullReport = new byte[547];
                            Array.Copy(outReport, fullReport, 78);
                            try
                            {
                                stream.SetFeature(fullReport);
                                sb.AppendLine("  SET_FEATURE ID=0x11 (547-byte output as feature): SUCCESS");
                            }
                            catch (Exception ex)
                            {
                                sb.AppendLine($"  SET_FEATURE ID=0x11 (547-byte output as feature): FAILED - {ex.Message}");
                            }
                        }
                    }
                    else
                    {
                        sb.AppendLine("  Failed to open device for feature report test");
                    }
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"  Error: {ex.Message}");
                }
                sb.AppendLine();
            }

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
            Log.Information("[HidDiagnostics] Written to {Path}", filePath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[HidDiagnostics] Failed to write diagnostics");
        }
    }

    private static ushort CalcCrc(byte[] data, int offset, int length)
    {
        ushort crc = 0xFFFF;
        for (int i = offset; i < offset + length; i++)
        {
            crc ^= (ushort)(data[i] << 8);
            for (int j = 0; j < 8; j++)
            {
                if ((crc & 0x8000) != 0)
                    crc = (ushort)((crc << 1) ^ 0x1021);
                else
                    crc <<= 1;
            }
        }
        return crc;
    }

    private static string DescribeDevice(HidDevice d)
    {
        return $"VID=0x{d.VendorID:X4} PID=0x{d.ProductID:X4} " +
               $"InLen={d.MaxInputReportLength} OutLen={d.MaxOutputReportLength} " +
               $"FeatLen={d.MaxFeatureReportLength} " +
               $"Manufacturer='{d.Manufacturer}' Product='{d.ProductName}' " +
               $"Serial='{d.SerialNumber}' Path='{d.DevicePath}'";
    }

    private static List<string> DescribeDescriptor(HidDevice device)
    {
        var lines = new List<string>();
        try
        {
            var descriptor = device.GetReportDescriptor();
            if (descriptor == null)
            {
                lines.Add("(no descriptor)");
                return lines;
            }

            lines.Add($"DeviceItems: {descriptor.DeviceItems?.Count ?? 0}");
            lines.Add($"ReportsUseID: {descriptor.ReportsUseID}");
            lines.Add($"InputReports: {descriptor.InputReports?.Count() ?? 0}");
            lines.Add($"OutputReports: {descriptor.OutputReports?.Count() ?? 0}");
            lines.Add($"FeatureReports: {descriptor.FeatureReports?.Count() ?? 0}");

            if (descriptor.DeviceItems != null)
            {
                for (int i = 0; i < descriptor.DeviceItems.Count; i++)
                {
                    var item = descriptor.DeviceItems[i];
                    var usages = GetUsages(item);
                    var collType = item.CollectionType;
                    lines.Add($"  DeviceItem[{i}]: Collection={collType}, Usages=[{string.Join(", ", usages)}]");

                    // Show report details for this device item
                    foreach (var report in item.Reports ?? Array.Empty<Report>())
                    {
                        var flags = new List<string>();
                        if (report.ReportID != 0)
                            flags.Add($"ID=0x{report.ReportID:X2}");

                        flags.Add($"Type={report.ReportType}");
                        flags.Add($"Length={report.Length}");

                        // Get usages from data items
                        var dataUsages = new List<string>();
                        foreach (var dataItem in report.DataItems ?? Array.Empty<DataItem>())
                        {
                            var itemUsages = GetUsages(dataItem);
                            dataUsages.AddRange(itemUsages);
                        }

                        if (dataUsages.Count > 0)
                            flags.Add($"DataUsages=[{string.Join(", ", dataUsages.Distinct())}]");

                        lines.Add($"    Report: {string.Join(", ", flags)}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            lines.Add($"(descriptor error: {ex.Message})");
        }

        return lines;
    }

    private static List<string> GetUsages(DescriptorItem item)
    {
        var result = new List<string>();
        try
        {
            if (item.Usages != null)
            {
                var values = item.Usages.GetAllValues();
                if (values != null)
                {
                    foreach (var val in values)
                    {
                        var usagePage = (val >> 16) & 0xFFFF;
                        var usage = val & 0xFFFF;
                        result.Add($"0x{usagePage:X4}:0x{usage:X4}");
                    }
                }
            }
        }
        catch
        {
            // ignore
        }

        return result;
    }
}
