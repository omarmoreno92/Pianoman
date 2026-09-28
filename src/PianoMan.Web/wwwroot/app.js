'use strict';

const $ = id => document.getElementById(id);
const MAX_BYTES = 2 * 1024 * 1024;
const pieces = {K:'♔',Q:'♕',R:'♖',B:'♗',N:'♘',P:'♙',k:'♚',q:'♛',r:'♜',b:'♝',n:'♞',p:'♟'};
const classEs = {best:'Mejor',excellent:'Excelente',good:'Buena',inaccuracy:'Imprecisión',mistake:'Error',blunder:'Error grave'};
const labels = {material:'Material',activity:'Actividad',coordination:'Coordinación',kingSafety:'Seguridad del rey',space:'Espacio',structure:'Estructura',pressure:'Presión',initiative:'Iniciativa'};
let payload = null, game = null, ply = 0, marks = [];
let audio = null;

const drop = $('dropZone');
$('pickFile').onclick = () => $('fileInput').click();
$('fileInput').onchange = e => loadFile(e.target.files[0]);
['dragenter','dragover'].forEach(name => drop.addEventListener(name, e => {e.preventDefault();drop.classList.add('drag');}));
['dragleave','drop'].forEach(name => drop.addEventListener(name, e => {e.preventDefault();drop.classList.remove('drag');}));
drop.addEventListener('drop', e => loadFile(e.dataTransfer.files[0]));
async function loadFile(file){if(!file)return;if(file.size>MAX_BYTES){showError('El archivo supera 2 MB.');return;}$('pgnText').value=await file.text();}
$('demoBtn').onclick = async () => {$('pgnText').value = await (await fetch('/api/demo')).text();await analyze();};
$('analyzeBtn').onclick = analyze;
$('gameSelect').onchange = e => selectGame(Number(e.target.value));

async function analyze(){
  showError(''); const pgn=$('pgnText').value.trim(); if(!pgn){showError('Pega o carga un PGN.');return;} if(new TextEncoder().encode(pgn).length>MAX_BYTES){showError('El PGN supera 2 MB.');return;}
  $('engineState').textContent='Afinando…';
  try{
    const res=await fetch('/api/analyze',{method:'POST',headers:{'content-type':'application/json'},body:JSON.stringify({pgn})});
    const data=await res.json();if(!res.ok)throw new Error(data.error||'No se pudo analizar.');payload=data;marks=[];
    const select=$('gameSelect');select.innerHTML='';data.games.forEach((g,i)=>{const o=document.createElement('option');o.value=i;o.textContent=g.title;select.appendChild(o);});select.hidden=data.games.length<2;selectGame(0);$('workspace').hidden=false;
  }catch(err){showError(err.message);}finally{$('engineState').textContent='Listo';}
}
function selectGame(index){game=payload.games[index];ply=0;render();}

function fenBoard(fen){
  const board=[]; for(const rank of fen.split(' ')[0].split('/')){const row=[];for(const c of rank){if(/\d/.test(c)){for(let i=0;i<Number(c);i++)row.push('');}else row.push(c);}board.push(row);}return board;
}
function renderBoard(entry){
  const b=$('board');b.innerHTML='';const rows=fenBoard(entry.fen);for(let visualRank=0;visualRank<8;visualRank++)for(let file=0;file<8;file++){
    const rank=7-visualRank;const name=String.fromCharCode(97+file)+(rank+1);const sq=document.createElement('div');sq.className='sq '+(((file+rank)&1)?'dark':'light');if(name===entry.from||name===entry.to)sq.classList.add('last');sq.dataset.square=name;sq.textContent=pieces[rows[visualRank][file]]||'';b.appendChild(sq);
  }
}
function render(){
  if(!game)return;const e=game.timeline[ply];renderBoard(e);$('plyLabel').textContent=ply===0?'Inicio':`Ply ${ply}/${game.timeline.length-1}`;$('moveTitle').textContent=ply===0?'Posición inicial':`${e.moveNumber}. ${e.san} · ${e.uci}`;
  const decision=e.decision; $('modeBadge').textContent=decision?.mode==='theory'?'TEORÍA · SIN BÚSQUEDA':decision?'Afinando…':(e.inTheory?'EN LIBRO':'POSICIÓN');
  $('opening').textContent=e.eco&&e.opening?`${e.eco} · ${e.opening}`:(decision?.eco&&decision?.opening?`${decision.eco} · ${decision.opening}`:'Sin identidad ECO/nombre para esta posición');
  $('fen').textContent=e.fen;$('chord').textContent=e.chord.symbol;$('midi').textContent=e.chord.midiNotes.join(', ');$('tension').textContent=e.harmony.tension;$('balance').textContent=e.harmony.relativeScore;
  renderComponents(e.harmony);renderDecision(e);renderTimeline();if($('autoPlay').checked)playChord(e.chord.midiNotes,0,0);
}
function renderComponents(h){
  const host=$('components');host.innerHTML='';const side=h.sideToMove==='white'?h.white:h.black;for(const [key,label] of Object.entries(labels)){const d=document.createElement('div');d.className='component';d.innerHTML=`<small>${label}</small><strong>${side[key]}</strong>`;host.appendChild(d);}
}
function renderDecision(entry){
  const d=entry.decision;const text=$('decisionText'),host=$('candidateList');host.innerHTML='';if(!d){text.textContent='La posición inicial todavía no evalúa una jugada realizada.';return;}
  if(d.mode==='theory')text.textContent=`Jugada teórica #${d.rank}. Selección por peso del corpus; sin pérdida armónica.`;
  else text.textContent=`Rango ${d.rank} · pérdida ${d.loss} · ${classEs[d.classification]||d.classification}. Clasificación del modelo Piano Man.`;
  d.candidates.forEach((c,index)=>{
    const row=document.createElement('div');row.className='candidate'+(c.played?' played':'');const value=d.mode==='theory'?`peso ${c.weight}`:`score ${c.score}`;const loss=c.loss==null?'—':`−${c.loss}`;
    row.innerHTML=`<strong>#${c.rank}</strong><input class="voicePick" type="checkbox" data-index="${index}" ${index<10?'checked':''}><div><strong>${escapeHtml(c.san)}</strong> <code>${escapeHtml(c.uci)}</code></div><span class="secondary">${value}</span><span class="secondary">${loss}</span><span class="secondary">${escapeHtml(c.chord.symbol)}</span><span class="secondary">T ${c.tension}</span><button class="listen">▶</button>`;
    row.querySelector('.listen').onclick=()=>playCandidate(entry,c,index,d.candidates.length);host.appendChild(row);
  });
}
function renderTimeline(){const t=$('timeline');t.innerHTML='';game.timeline.forEach((e,i)=>{const b=document.createElement('button');b.textContent=i===0?'•':e.san;b.title=e.fen;if(i===ply)b.classList.add('active');b.onclick=()=>{ply=i;render();};t.appendChild(b);});t.children[ply]?.scrollIntoView({block:'nearest',inline:'nearest'});}

function ensureAudio(){audio ??= new (window.AudioContext||window.webkitAudioContext)();return audio;}
function midiHz(n){return 440*Math.pow(2,(n-69)/12);}
function playNotes(notes,start,duration,pan=0,gain=.055,arp=false){const ctx=ensureAudio();const now=ctx.currentTime+start;notes.forEach((m,i)=>{const osc=ctx.createOscillator(),g=ctx.createGain(),p=ctx.createStereoPanner();osc.type='sine';osc.frequency.value=midiHz(m);p.pan.value=pan;g.gain.setValueAtTime(0,now+(arp?i*.08:0));g.gain.linearRampToValueAtTime(gain,now+.02+(arp?i*.08:0));g.gain.exponentialRampToValueAtTime(.0001,now+duration+(arp?i*.08:0));osc.connect(g).connect(p).connect(ctx.destination);osc.start(now+(arp?i*.08:0));osc.stop(now+duration+.2+(arp?i*.08:0));});}
function playChord(notes,pan=0,delay=0){const beat=60/Number($('tempo').value);playNotes(notes,delay,beat*.85,pan,.055,$('playMode').value==='arp');}
function playCandidate(entry,c,index,total){const pan=total<=1?0:-.85+1.7*(index/(total-1));const beat=60/Number($('tempo').value);playNotes(entry.chord.midiNotes,0,beat*.65,pan,.04,$('playMode').value==='arp');playNotes(c.chord.midiNotes,beat*.72,beat*.8,pan,.05,$('playMode').value==='arp');}
$('playPosition').onclick=()=>game&&playChord(game.timeline[ply].chord.midiNotes);
$('compareBtn').onclick=()=>{if(!game)return;const e=game.timeline[ply],d=e.decision;if(!d)return;const picks=[...document.querySelectorAll('.voicePick:checked')].slice(0,10).map(x=>Number(x.dataset.index));picks.forEach((idx,i)=>playCandidate(e,d.candidates[idx],i,picks.length));};
$('tempo').oninput=e=>$('tempoValue').textContent=e.target.value;

function go(where){if(!game)return;const max=game.timeline.length-1;ply=where==='start'?0:where==='end'?max:where==='prev'?Math.max(0,ply-1):Math.min(max,ply+1);render();}
document.querySelectorAll('[data-nav]').forEach(b=>b.onclick=()=>go(b.dataset.nav));
document.addEventListener('keydown',e=>{if(e.target.matches('textarea,input,select'))return;if(e.key==='ArrowLeft'){e.preventDefault();go('prev');}if(e.key==='ArrowRight'){e.preventDefault();go('next');}});
$('copyFen').onclick=async()=>{if(game)await navigator.clipboard.writeText(game.timeline[ply].fen);};

$('markDissonance').onclick=()=>{
  if(!game||ply===0)return;const entry=game.timeline[ply];const observation={game:{title:game.title,event:game.event,white:game.white,black:game.black,result:game.result},ply,timestamp:new Date().toISOString(),san:entry.san,uci:entry.uci,fen:entry.fen,chord:entry.chord.symbol,midiNotes:entry.chord.midiNotes,harmonicVector:{white:entry.harmony.white,black:entry.harmony.black,balance:entry.harmony.relativeScore},tension:entry.harmony.tension,opening:{eco:entry.eco,name:entry.opening,inTheory:entry.inTheory},decision:entry.decision};marks=marks.filter(x=>x.ply!==ply);marks.push(observation);$('markDissonance').textContent='✓ Disonancia marcada';setTimeout(()=>$('markDissonance').textContent='Marcar disonancia',1000);
};
$('exportMarks').onclick=()=>{
  if(!game)return;const exportData={format:'pianoman-auditory-observations-v1',exportedAt:new Date().toISOString(),game:{title:game.title,event:game.event,white:game.white,black:game.black,result:game.result},markedPlies:marks.map(x=>x.ply),observations:marks,timeline:game.timeline.map(e=>({ply:e.ply,san:e.san,uci:e.uci,fen:e.fen,chord:e.chord.symbol,midiNotes:e.chord.midiNotes,harmonicVector:{white:e.harmony.white,black:e.harmony.black,balance:e.harmony.relativeScore},tension:e.harmony.tension,opening:{eco:e.eco,name:e.opening,inTheory:e.inTheory},decision:e.decision}))};
  const blob=new Blob([JSON.stringify(exportData,null,2)],{type:'application/json'});const a=document.createElement('a');a.href=URL.createObjectURL(blob);a.download='pianoman-dissonance-observations.json';a.click();setTimeout(()=>URL.revokeObjectURL(a.href),1000);
};
function showError(v){$('error').textContent=v;}
function escapeHtml(v){return String(v??'').replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));}
