namespace NovaPad.WPF.Helpers;

public static class GamepadTestPage
{
    public static string GetHtml() => @"<!DOCTYPE html>
<html lang=""es"">
<head>
<meta charset=""UTF-8"">
<meta name=""viewport"" content=""width=device-width,initial-scale=1.0"">
<title>Gamepad Tester</title>
<style>
  *{margin:0;padding:0;box-sizing:border-box}
  body{font-family:'Segoe UI',sans-serif;background:#1a1a2e;color:#e0e0e0;display:flex;align-items:center;justify-content:center;min-height:100vh;padding:12px}
  .container{width:100%;max-width:760px}
  .status{text-align:center;padding:14px;border-radius:10px;background:#16213e;margin-bottom:12px;font-size:13px;font-weight:600;letter-spacing:.3px}
  .status.connected{border:1px solid #00c853;color:#00c853;background:#1a2e1a}
  .status.disconnected{border:1px solid #ff5252;color:#ff5252;background:#2e1a1a}

  .sticks{display:flex;gap:12px;margin-bottom:12px;justify-content:center}
  .stick-wrap{flex:1;max-width:280px;text-align:center;background:#16213e;border-radius:12px;padding:16px;border:1px solid #0f3460}
  .stick-wrap h3{font-size:10px;color:#7f8fa6;margin-bottom:10px;text-transform:uppercase;letter-spacing:1px}
  .stick-bg{width:140px;height:140px;border-radius:70px;background:#0f3460;position:relative;margin:0 auto;border:2px solid #1a3a6a}

  .stick-cross{position:absolute;top:0;left:0;width:100%;height:100%;pointer-events:none}
  .stick-cross::before{content:'';position:absolute;top:50%;left:0;right:0;height:1px;background:rgba(255,255,255,.08);transform:translateY(-50%)}
  .stick-cross::after{content:'';position:absolute;left:50%;top:0;bottom:0;width:1px;background:rgba(255,255,255,.08);transform:translateX(-50%)}
  .stick-ring{position:absolute;top:50%;left:50%;width:90px;height:90px;border-radius:45px;border:1px solid rgba(255,255,255,.06);transform:translate(-50%,-50%)}
  .stick-ring2{position:absolute;top:50%;left:50%;width:45px;height:45px;border-radius:23px;border:1px solid rgba(255,255,255,.04);transform:translate(-50%,-50%)}

  .stick-dot{width:18px;height:18px;border-radius:9px;background:#e94560;position:absolute;z-index:2;left:50%;top:50%;transform:translate(-50%,-50%);box-shadow:0 0 8px rgba(233,69,96,.5);transition:box-shadow .1s}
  .stick-dot-inner{width:8px;height:8px;border-radius:4px;background:#fff;position:absolute;top:5px;left:5px;opacity:.6}
  .coord{font-size:10px;color:#7f8fa6;margin-top:6px;font-family:'Consolas','Courier New',monospace;letter-spacing:.5px}

  .triggers{display:flex;gap:12px;margin-bottom:12px}
  .trigger-wrap{flex:1;background:#16213e;border-radius:12px;padding:14px;border:1px solid #0f3460}
  .trigger-header{display:flex;justify-content:space-between;margin-bottom:8px}
  .trigger-header h3{font-size:10px;color:#7f8fa6;text-transform:uppercase;letter-spacing:1px}
  .trigger-val{font-size:11px;color:#e0e0e0;font-family:'Consolas','Courier New',monospace;font-weight:600}
  .trigger-bar{height:6px;background:#0f3460;border-radius:3px;overflow:hidden}
  .trigger-fill{height:100%;border-radius:3px;transition:width .03s}

  .buttons{display:grid;grid-template-columns:repeat(6,1fr);gap:6px;margin-bottom:12px}
  .btn{padding:10px 2px;text-align:center;border-radius:8px;font-size:10px;font-weight:700;background:#16213e;border:1px solid #0f3460;transition:all .08s;color:#7f8fa6}
  .btn.active{background:#e94560;border-color:#e94560;color:#fff;box-shadow:0 0 10px rgba(233,69,96,.4)}
  .btn .label{display:block;font-size:8px;font-weight:400;color:#5a6a7a;margin-bottom:2px}
  .btn.active .label{color:rgba(255,255,255,.6)}

  .meta{text-align:center;font-size:11px;color:#7f8fa6;padding:10px;background:#16213e;border-radius:10px;border:1px solid #0f3460}
  .meta span{margin:0 10px}
  .meta .dot{color:#e94560}
</style>
</head>
<body>
<div class=""container"">
  <div class=""status disconnected"" id=""status"">Esperando mando...</div>

  <div class=""sticks"">
    <div class=""stick-wrap"">
      <h3>Izquierdo</h3>
      <div class=""stick-bg"">
        <div class=""stick-cross""></div>
        <div class=""stick-ring""></div>
        <div class=""stick-ring2""></div>
        <div class=""stick-dot"" id=""ls""><div class=""stick-dot-inner""></div></div>
      </div>
      <div class=""coord"" id=""lsCoord"">0.00, 0.00</div>
    </div>
    <div class=""stick-wrap"">
      <h3>Derecho</h3>
      <div class=""stick-bg"">
        <div class=""stick-cross""></div>
        <div class=""stick-ring""></div>
        <div class=""stick-ring2""></div>
        <div class=""stick-dot"" id=""rs""><div class=""stick-dot-inner""></div></div>
      </div>
      <div class=""coord"" id=""rsCoord"">0.00, 0.00</div>
    </div>
  </div>

  <div class=""triggers"">
    <div class=""trigger-wrap"">
      <div class=""trigger-header""><h3>LT</h3><span class=""trigger-val"" id=""ltVal"">0</span></div>
      <div class=""trigger-bar""><div class=""trigger-fill"" id=""ltFill"" style=""width:0%""></div></div>
    </div>
    <div class=""trigger-wrap"">
      <div class=""trigger-header""><h3>RT</h3><span class=""trigger-val"" id=""rtVal"">0</span></div>
      <div class=""trigger-bar""><div class=""trigger-fill"" id=""rtFill"" style=""width:0%""></div></div>
    </div>
  </div>

  <div class=""buttons"" id=""buttons""></div>
  <div class=""meta"" id=""meta"">Esperando datos...</div>
</div>

<script>
  const BTN_DEFS = [
    ['A','A'],['B','B'],['X','X'],['Y','Y'],
    ['LB','LB'],['RB','RB'],['LT','LT'],['RT','RT'],
    ['Back','Sel'],['Start','St'],['LS','LS'],['RS','RS'],
    ['D-Up','\u2191'],['D-Dn','\u2193'],['D-L','\u2190'],['D-R','\u2192'],
    ['Guide','PS'],['Touch','Tch']
  ];
  const container = document.getElementById('buttons');
  BTN_DEFS.forEach((d,i)=>{
    const el=document.createElement('div'); el.className='btn'; el.id='b'+i;
    el.innerHTML='<span class=""label"">'+d[0]+'</span>'+d[1]; container.appendChild(el);
  });

  const S=document.getElementById('status'),M=document.getElementById('meta');
  const ls=document.getElementById('ls'),rs=document.getElementById('rs');
  const lc=document.getElementById('lsCoord'),rc=document.getElementById('rsCoord');
  const lF=document.getElementById('ltFill'),rF=document.getElementById('rtFill');
  const lV=document.getElementById('ltVal'),rV=document.getElementById('rtVal');
  const RANGE=50;
  const TRIGGER_COLORS=['#e94560','#ff6b35','#ffd93d','#6bcb77','#4fc3f7'];

  function trigColor(v){return v<.25?TRIGGER_COLORS[0]:v<.5?TRIGGER_COLORS[1]:v<.75?TRIGGER_COLORS[2]:v<.9?TRIGGER_COLORS[3]:TRIGGER_COLORS[4]}

  function update(){
    const gp=navigator.getGamepads()[0];
    if(!gp){S.className='status disconnected';S.textContent='Desconectado';M.textContent='Esperando mando...';return;}
    S.className='status connected';
    S.textContent=gp.id;

    const lx=gp.axes[0]||0,ly=gp.axes[1]||0;
    const rx=gp.axes[2]||0,ry=gp.axes[3]||0;
    ls.style.transform='translate(calc(-50% + '+(lx*RANGE)+'px),calc(-50% + '+(ly*RANGE)+'px))';
    rs.style.transform='translate(calc(-50% + '+(rx*RANGE)+'px),calc(-50% + '+(ry*RANGE)+'px))';
    lc.textContent=(lx>=0?'+':'')+lx.toFixed(2)+', '+(ly>=0?'+':'')+ly.toFixed(2);
    rc.textContent=(rx>=0?'+':'')+rx.toFixed(2)+', '+(ry>=0?'+':'')+ry.toFixed(2);

    const lt=gp.buttons[6]?.value||0,rt=gp.buttons[7]?.value||0;
    lF.style.width=(lt*100)+'%';lF.style.background=trigColor(lt);
    rF.style.width=(rt*100)+'%';rF.style.background=trigColor(rt);
    lV.textContent=(lt*100).toFixed(0);rV.textContent=(rt*100).toFixed(0);

    for(let i=0;i<18;i++){
      const el=document.getElementById('b'+i);
      if(!el)continue;
      const p=gp.buttons[i]?.pressed||false;
      el.className='btn'+(p?' active':'');
    }

    M.innerHTML='<span>Ejes: <b class=""dot"">'+gp.axes.length+'</b></span><span>Botones: <b class=""dot"">'+gp.buttons.length+'</b></span>';
    requestAnimationFrame(update);
  }

  window.addEventListener('gamepadconnected',()=>{S.className='status connected';update();});
  window.addEventListener('gamepaddisconnected',()=>{S.className='status disconnected';S.textContent='Desconectado';});

</script>
</body>
</html>";
}
