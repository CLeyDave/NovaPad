namespace NovaPad.WPF.Helpers;

public static class GamepadTestPagePS
{
    public static string GetHtml() => @"<!DOCTYPE html>
<html lang=""es"">
<head>
<meta charset=""UTF-8"">
<meta name=""viewport"" content=""width=device-width,initial-scale=1.0"">
<title>Gamepad Tester PS</title>
<style>
  *{margin:0;padding:0;box-sizing:border-box}
  body{font-family:'Segoe UI',sans-serif;background:#1a1a2e;color:#e0e0e0;display:flex;align-items:center;justify-content:center;min-height:100vh;padding:12px}
  .container{width:100%;max-width:1100px}

  .status{text-align:center;padding:10px;border-radius:8px;background:#16213e;margin-bottom:10px;font-size:12px;font-weight:600;letter-spacing:.3px}
  .status.connected{border:1px solid #00c853;color:#00c853;background:#1a2e1a}
  .status.disconnected{border:1px solid #ff5252;color:#ff5252;background:#2e1a1a}

  .trigger-group{display:flex;flex-direction:column;align-items:center;gap:10px;background:#16213e;border-radius:8px;padding:10px;border:1px solid #0f3460}
  .trigger-rect{width:110px;height:140px;border-radius:10px;background:#0f3460;border:1px solid #1a3a6a;position:relative;overflow:hidden;flex-shrink:0}
  .trigger-fill{position:absolute;bottom:0;left:0;width:100%;border-radius:0 0 8px 8px;z-index:1;transition:height .03s}
  .trigger-labels{position:absolute;bottom:4px;left:0;width:100%;text-align:center;z-index:2;pointer-events:none}
  .trigger-labels .trigger-val{display:block;font-size:13px;color:#e0e0e0;font-family:'Consolas','Courier New',monospace;font-weight:600;text-shadow:0 1px 3px rgba(0,0,0,.7)}
  .trigger-labels .trigger-label{display:block;font-size:9px;color:rgba(255,255,255,.5);text-transform:uppercase;letter-spacing:1px;text-shadow:0 1px 3px rgba(0,0,0,.7)}
  .shoulder-btn{width:110px;height:48px;border-radius:8px;background:#16213e;border:1px solid #0f3460;display:flex;align-items:center;justify-content:center;font-size:14px;font-weight:700;color:#7f8fa6;transition:all .08s;flex-shrink:0}
  .shoulder-btn.active{background:#e94560;border-color:#e94560;color:#fff;box-shadow:0 0 8px rgba(233,69,96,.4)}

  .main-square{background:#16213e;border-radius:12px;padding:12px;border:1px solid #0f3460;margin-bottom:8px}

  .top-row{display:flex;align-items:center;gap:8px;margin-bottom:12px;justify-content:center}
  .sys-btn{width:72px;height:32px;border-radius:8px;background:#16213e;border:1px solid #0f3460;display:flex;align-items:center;justify-content:center;font-size:12px;font-weight:700;color:#7f8fa6;transition:all .08s;cursor:default}
  .sys-btn.active{background:#e94560;border-color:#e94560;color:#fff;box-shadow:0 0 8px rgba(233,69,96,.4)}
  .touchpad{flex:1;max-width:200px;height:40px;border-radius:8px;background:#0f3460;border:1px solid #1a3a6a;display:flex;align-items:center;justify-content:center;font-size:11px;font-weight:600;color:#5a6a7a;transition:all .08s;cursor:default;user-select:none}
  .touchpad.active{background:#e94560;border-color:#e94560;color:#fff;box-shadow:0 0 8px rgba(233,69,96,.4)}

  .game-area{display:flex;justify-content:space-evenly;align-items:center;margin-bottom:0;padding:0}

  .dpad-grid{display:grid;grid-template-columns:repeat(3,58px);grid-template-rows:repeat(3,58px);gap:4px;align-items:center;justify-items:center}
  .dpad-btn{width:56px;height:56px;border-radius:8px;background:#0f3460;border:1px solid #1a3a6a;display:flex;align-items:center;justify-content:center;transition:all .08s;cursor:default;user-select:none;position:relative}
  .dpad-btn.active{background:#e94560;border-color:#e94560;box-shadow:0 0 10px rgba(233,69,96,.5)}
  .dpad-btn svg{stroke:#7f8fa6;stroke-width:2.5;fill:none;width:26px;height:26px;stroke-linecap:round;stroke-linejoin:round}
  .dpad-btn.active svg{stroke:#fff}

  #b12::after{content:'';position:absolute;bottom:-28px;left:50%;transform:translateX(-50%);width:16px;height:28px;background:url('data:image/svg+xml,%3Csvg xmlns=''http://www.w3.org/2000/svg'' viewBox=''0 0 16 24''%3E%3Cpath d=''M0 0 L8 24 L16 0 Z'' fill=''%230f3460'' stroke=''%230f3460'' stroke-width=''3'' stroke-linejoin=''round''/%3E%3C/svg%3E') no-repeat center/contain;z-index:1;pointer-events:none}
  #b12.active::after{background:url('data:image/svg+xml,%3Csvg xmlns=''http://www.w3.org/2000/svg'' viewBox=''0 0 16 24''%3E%3Cpath d=''M0 0 L8 24 L16 0 Z'' fill=''%23e94560'' stroke=''%23e94560'' stroke-width=''3'' stroke-linejoin=''round''/%3E%3C/svg%3E') no-repeat center/contain}
  #b13::after{content:'';position:absolute;top:-28px;left:50%;transform:translateX(-50%);width:16px;height:28px;background:url('data:image/svg+xml,%3Csvg xmlns=''http://www.w3.org/2000/svg'' viewBox=''0 0 16 24''%3E%3Cpath d=''M0 24 L8 0 L16 24 Z'' fill=''%230f3460'' stroke=''%230f3460'' stroke-width=''3'' stroke-linejoin=''round''/%3E%3C/svg%3E') no-repeat center/contain;z-index:1;pointer-events:none}
  #b13.active::after{background:url('data:image/svg+xml,%3Csvg xmlns=''http://www.w3.org/2000/svg'' viewBox=''0 0 16 24''%3E%3Cpath d=''M0 24 L8 0 L16 24 Z'' fill=''%23e94560'' stroke=''%23e94560'' stroke-width=''3'' stroke-linejoin=''round''/%3E%3C/svg%3E') no-repeat center/contain}
  #b14::after{content:'';position:absolute;right:-24px;top:50%;transform:translateY(-50%);width:24px;height:16px;background:url('data:image/svg+xml,%3Csvg xmlns=''http://www.w3.org/2000/svg'' viewBox=''0 0 24 16''%3E%3Cpath d=''M0 0 L24 8 L0 16 Z'' fill=''%230f3460'' stroke=''%230f3460'' stroke-width=''3'' stroke-linejoin=''round''/%3E%3C/svg%3E') no-repeat center/contain;z-index:1;pointer-events:none}
  #b14.active::after{background:url('data:image/svg+xml,%3Csvg xmlns=''http://www.w3.org/2000/svg'' viewBox=''0 0 24 16''%3E%3Cpath d=''M0 0 L24 8 L0 16 Z'' fill=''%23e94560'' stroke=''%23e94560'' stroke-width=''3'' stroke-linejoin=''round''/%3E%3C/svg%3E') no-repeat center/contain}
  #b15::after{content:'';position:absolute;left:-24px;top:50%;transform:translateY(-50%);width:24px;height:16px;background:url('data:image/svg+xml,%3Csvg xmlns=''http://www.w3.org/2000/svg'' viewBox=''0 0 24 16''%3E%3Cpath d=''M24 0 L0 8 L24 16 Z'' fill=''%230f3460'' stroke=''%230f3460'' stroke-width=''3'' stroke-linejoin=''round''/%3E%3C/svg%3E') no-repeat center/contain;z-index:1;pointer-events:none}
  #b15.active::after{background:url('data:image/svg+xml,%3Csvg xmlns=''http://www.w3.org/2000/svg'' viewBox=''0 0 24 16''%3E%3Cpath d=''M24 0 L0 8 L24 16 Z'' fill=''%23e94560'' stroke=''%23e94560'' stroke-width=''3'' stroke-linejoin=''round''/%3E%3C/svg%3E') no-repeat center/contain}

  .face-grid{display:grid;grid-template-columns:repeat(3,58px);grid-template-rows:repeat(3,58px);gap:4px;align-items:center;justify-items:center}
  .face-btn{width:56px;height:56px;border-radius:28px;background:#0f3460;border:1px solid #1a3a6a;display:flex;align-items:center;justify-content:center;transition:all .08s;cursor:default;user-select:none}
  .face-btn.active{background:#e94560;border-color:#e94560;box-shadow:0 0 10px rgba(233,69,96,.5)}
  .face-btn svg{stroke:#7f8fa6;stroke-width:2.5;fill:none;width:26px;height:26px}
  .face-btn.active svg{stroke:#fff}
  .face-btn.square svg{stroke:#7f8fa6;stroke-width:2.5;fill:none;width:24px;height:24px}
  .face-btn.circle svg{stroke:#7f8fa6;stroke-width:2.5;fill:none;width:24px;height:24px}
  .face-btn.cross svg{stroke:#7f8fa6;stroke-width:3;fill:none;width:28px;height:28px}
  .face-btn.tri svg{stroke:#7f8fa6;stroke-width:2.5;fill:none;width:24px;height:24px}

  .sticks-row{display:flex;gap:10px;justify-content:center;align-items:center;margin-top:20px}
  .stick-wrap{width:150px;height:150px;border-radius:50%;background:#0f3460;border:1px solid #1a3a6a;display:flex;flex-direction:column;align-items:center;justify-content:center}
  .stick-wrap h3{font-size:9px;color:#7f8fa6;margin-bottom:4px;text-transform:uppercase;letter-spacing:1px}
  .stick-bg{width:110px;height:110px;border-radius:55px;background:#0a1a3a;position:relative;margin:0 auto;border:2px solid #1a3a6a}
  .stick-cross{position:absolute;top:0;left:0;width:100%;height:100%;pointer-events:none}
  .stick-cross::before{content:'';position:absolute;top:50%;left:0;right:0;height:1px;background:rgba(255,255,255,.08);transform:translateY(-50%)}
  .stick-cross::after{content:'';position:absolute;left:50%;top:0;bottom:0;width:1px;background:rgba(255,255,255,.08);transform:translateX(-50%)}
  .stick-ring{position:absolute;top:50%;left:50%;width:70px;height:70px;border-radius:35px;border:1px solid rgba(255,255,255,.06);transform:translate(-50%,-50%)}
  .stick-ring2{position:absolute;top:50%;left:50%;width:35px;height:35px;border-radius:18px;border:1px solid rgba(255,255,255,.04);transform:translate(-50%,-50%)}
  .stick-dot{width:16px;height:16px;border-radius:8px;background:#e94560;position:absolute;z-index:2;left:50%;top:50%;transform:translate(-50%,-50%);box-shadow:0 0 8px rgba(233,69,96,.5);transition:box-shadow .1s, background .1s}
  .stick-dot.clicked{background:#fff;box-shadow:0 0 12px rgba(255,255,255,.7)}
  .stick-dot-inner{width:7px;height:7px;border-radius:4px;background:#fff;position:absolute;top:4.5px;left:4.5px;opacity:.6}
  .coord{font-size:9px;color:#7f8fa6;margin-top:4px;font-family:'Consolas','Courier New',monospace;letter-spacing:.5px}

  .ps-btn{width:48px;height:48px;border-radius:24px;background:#0f3460;border:1px solid #1a3a6a;display:flex;align-items:center;justify-content:center;transition:all .08s;cursor:default;user-select:none;flex-shrink:0;margin:0 4px}
  .ps-btn.active{background:#e94560;border-color:#e94560;box-shadow:0 0 10px rgba(233,69,96,.5)}
  .ps-btn svg{stroke:#7f8fa6;stroke-width:2.5;fill:none;width:20px;height:20px}
  .ps-btn.active svg{stroke:#fff}

  .meta{text-align:center;font-size:10px;color:#7f8fa6;padding:8px;background:#16213e;border-radius:8px;border:1px solid #0f3460}
  .meta span{margin:0 8px}
  .meta .dot{color:#e94560}
</style>
</head>
<body>
<div class=""container"">
  <div class=""status disconnected"" id=""status"">Esperando mando...</div>

  <div class=""main-square"">
    <div class=""top-row"">
      <div class=""sys-btn"" id=""b8"">Select</div>
      <div class=""touchpad"" id=""b17"">Touchpad</div>
      <div class=""sys-btn"" id=""b9"">Start</div>
    </div>

    <div class=""game-area"">
      <div class=""trigger-group"">
        <div class=""trigger-rect"">
          <div class=""trigger-fill"" id=""ltFill"" style=""height:0%""></div>
          <div class=""trigger-labels"">
            <span class=""trigger-val"" id=""ltVal"">0</span>
            <span class=""trigger-label"">L2</span>
          </div>
        </div>
        <div class=""shoulder-btn"" id=""b4"">L1</div>
      </div>
      <div class=""dpad-grid"">
        <div></div>
        <div class=""dpad-btn"" id=""b12""><svg viewBox=""0 0 24 24""><path d=""M12 3L7 10L10 10L10 20L14 20L14 10L17 10Z""/></svg></div>
        <div></div>
        <div class=""dpad-btn"" id=""b14""><svg viewBox=""0 0 24 24""><path d=""M3 12L10 7L10 10L20 10L20 14L10 14L10 17Z""/></svg></div>
        <div></div>
        <div class=""dpad-btn"" id=""b15""><svg viewBox=""0 0 24 24""><path d=""M21 12L14 7L14 10L4 10L4 14L14 14L14 17Z""/></svg></div>
        <div></div>
        <div class=""dpad-btn"" id=""b13""><svg viewBox=""0 0 24 24""><path d=""M12 21L7 14L10 14L10 4L14 4L14 14L17 14Z""/></svg></div>
        <div></div>
      </div>
      <div class=""face-grid"">
        <div></div>
        <div class=""face-btn tri"" id=""b3""><svg viewBox=""0 0 24 24""><polygon points=""12 3 3 21 21 21""></polygon></svg></div>
        <div></div>
        <div class=""face-btn square"" id=""b2""><svg viewBox=""0 0 24 24""><rect x=""4"" y=""4"" width=""16"" height=""16"" rx=""2""></rect></svg></div>
        <div></div>
        <div class=""face-btn circle"" id=""b1""><svg viewBox=""0 0 24 24""><circle cx=""12"" cy=""12"" r=""9""></circle></svg></div>
        <div></div>
        <div class=""face-btn cross"" id=""b0""><svg viewBox=""0 0 24 24""><line x1=""18"" y1=""6"" x2=""6"" y2=""18""></line><line x1=""6"" y1=""6"" x2=""18"" y2=""18""></line></svg></div>
        <div></div>
      </div>
      <div class=""trigger-group right"">
        <div class=""trigger-rect"">
          <div class=""trigger-fill"" id=""rtFill"" style=""height:0%""></div>
          <div class=""trigger-labels"">
            <span class=""trigger-val"" id=""rtVal"">0</span>
            <span class=""trigger-label"">R2</span>
          </div>
        </div>
        <div class=""shoulder-btn"" id=""b5"">R1</div>
      </div>
    </div>

    <div class=""sticks-row"">
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
      <div class=""ps-btn"" id=""b16""><svg viewBox=""0 0 24 24""><path d=""M12 2L2 7l10 5 10-5-10-5z M2 17l10 5 10-5 M2 12l10 5 10-5""></path></svg></div>
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
  </div>

  <div class=""meta"" id=""meta"">Esperando datos...</div>
</div>

<script>
  const DEADZONE = 0.03;
  function dz(v){return Math.abs(v)<DEADZONE?0:v}

  const S=document.getElementById('status'),M=document.getElementById('meta');
  const ls=document.getElementById('ls'),rs=document.getElementById('rs');
  const lc=document.getElementById('lsCoord'),rc=document.getElementById('rsCoord');
  const lF=document.getElementById('ltFill'),rF=document.getElementById('rtFill');
  const lV=document.getElementById('ltVal'),rV=document.getElementById('rtVal');
  const RANGE=42;
  const TRIGGER_COLORS=['#e94560','#ff6b35','#ffd93d','#6bcb77','#4fc3f7'];

  function trigColor(v){return v<.25?TRIGGER_COLORS[0]:v<.5?TRIGGER_COLORS[1]:v<.75?TRIGGER_COLORS[2]:v<.9?TRIGGER_COLORS[3]:TRIGGER_COLORS[4]}

  function update(){
    const gp=navigator.getGamepads()[0];
    if(!gp){S.className='status disconnected';S.textContent='Desconectado';M.textContent='Esperando mando...';return;}
    S.className='status connected';
    S.textContent=gp.id;

    const lx=dz(gp.axes[0]||0),ly=dz(gp.axes[1]||0);
    const rx=dz(gp.axes[2]||0),ry=dz(gp.axes[3]||0);
    ls.style.transform='translate(calc(-50% + '+(lx*RANGE)+'px),calc(-50% + '+(ly*RANGE)+'px))';
    rs.style.transform='translate(calc(-50% + '+(rx*RANGE)+'px),calc(-50% + '+(ry*RANGE)+'px))';
    lc.textContent=(lx>=0?'+':'')+lx.toFixed(2)+', '+(ly>=0?'+':'')+ly.toFixed(2);
    rc.textContent=(rx>=0?'+':'')+rx.toFixed(2)+', '+(ry>=0?'+':'')+ry.toFixed(2);

    const lt=gp.buttons[6]?.value||0,rt=gp.buttons[7]?.value||0;
    lF.style.height=(lt*100)+'%';lF.style.background=trigColor(lt);
    rF.style.height=(rt*100)+'%';rF.style.background=trigColor(rt);
    lV.textContent=(lt*100).toFixed(0);rV.textContent=(rt*100).toFixed(0);

    ls.classList.toggle('clicked',!!gp.buttons[10]?.pressed);
    rs.classList.toggle('clicked',!!gp.buttons[11]?.pressed);

    const BTN_IDS = ['b0','b1','b2','b3','b4','b5','b6','b7','b8','b9','b10','b11','b12','b13','b14','b15','b16','b17'];
    for(let i=0;i<BTN_IDS.length;i++){
      const el=document.getElementById(BTN_IDS[i]);
      if(!el)continue;
      const p=gp.buttons[i]?.pressed||false;
      el.className=el.className.replace(' active','')+(p?' active':'');
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
