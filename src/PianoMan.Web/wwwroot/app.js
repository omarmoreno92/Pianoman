'use strict';

const $ = id => document.getElementById(id);
const MAX_BYTES = 2 * 1024 * 1024;
const pieces = {K:'♔',Q:'♕',R:'♖',B:'♗',N:'♘',P:'♙',k:'♚',q:'♛',r:'♜',b:'♝',n:'♞',p:'♟'};
const classEs = {best:'Mejor',excellent:'Excelente',good:'Buena',inaccuracy:'Imprecisión',mistake:'Error',blunder:'Error grave'};
const labels = {material:'Material',activity:'Actividad',coordination:'Coordinación',kingSafety:'Seguridad del rey',space:'Espacio',structure:'Estructura',pressure:'Presión',initiative:'Iniciativa'};
let payload = null, game = null, ply = 0, marks = [], selectedSquare = null, moving = false;
let audio = null, audioMaster = null;
const noteNames = ['C','C♯','D','D♯','E','F','F♯','G','G♯','A','A♯','B'];

const drop = $('dropZone');
$('pickFile').onclick = () => $('fileInput').click();
$('fileInput').onchange = e => loadFile(e.target.files[0]);
['dragenter','dragover'].forEach(name => drop.addEventListener(name, e => {e.preventDefault();drop.classList.add('drag');}));
['dragleave','drop'].forEach(name => drop.addEventListener(name, e => {e.preventDefault();drop.classList.remove('drag');}));
drop.addEventListener('drop', e => loadFile(e.dataTransfer.files[0]));
async function loadFile(file){if(!file)return;if(file.size>MAX_BYTES){showError('El archivo supera 2 MB.');return;}$('pgnText').value=await file.text();}
document.querySelectorAll('[data-demo]').forEach(button=>button.onclick=async()=>{$('pgnText').value=await (await fetch(button.dataset.demo)).text();await analyze();});
$('analyzeBtn').onclick = analyze;
$('gameSelect').onchange = e => selectGame(Number(e.target.value));
$('listeningPerspective').onchange = () => render();

async function analyze(){
  showError(''); const pgn=$('pgnText').value.trim(); if(!pgn){showError('Pega o carga un PGN.');return;} if(new TextEncoder().encode(pgn).length>MAX_BYTES){showError('El PGN supera 2 MB.');return;}
  $('engineState').textContent='Afinando…';
  try{
    const res=await fetch('/api/analyze',{method:'POST',headers:{'content-type':'application/json'},body:JSON.stringify({pgn})});
    const data=await res.json();if(!res.ok)throw new Error(data.error||'No se pudo analizar.');payload=data;marks=[];
    const select=$('gameSelect');select.innerHTML='';data.games.forEach((g,i)=>{const o=document.createElement('option');o.value=i;o.textContent=g.title;select.appendChild(o);});select.hidden=data.games.length<2;selectGame(0);$('workspace').hidden=false;
  }catch(err){showError(err.message);}finally{$('engineState').textContent='Listo';}
}
function selectGame(index){game=typeof structuredClone==='function'?structuredClone(payload.games[index]):JSON.parse(JSON.stringify(payload.games[index]));ply=0;selectedSquare=null;$('resetBranch').hidden=true;render();}

function fenBoard(fen){
  const board=[]; for(const rank of fen.split(' ')[0].split('/')){const row=[];for(const c of rank){if(/\d/.test(c)){for(let i=0;i<Number(c);i++)row.push('');}else row.push(c);}board.push(row);}return board;
}
function renderBoard(entry){
  const b=$('board');b.innerHTML='';const rows=fenBoard(entry.fen),legal=entry.legalMoves||[],targets=new Set(selectedSquare?legal.filter(m=>m.from===selectedSquare).map(m=>m.to):[]);for(let visualRank=0;visualRank<8;visualRank++)for(let file=0;file<8;file++){
    const rank=7-visualRank;const name=String.fromCharCode(97+file)+(rank+1);const sq=document.createElement('button');sq.type='button';sq.className='sq '+(((file+rank)&1)?'dark':'light');if(name===entry.from||name===entry.to)sq.classList.add('last');if(name===selectedSquare)sq.classList.add('selected');if(targets.has(name))sq.classList.add('target');if(legal.some(m=>m.from===name))sq.classList.add('movable');sq.dataset.square=name;sq.textContent=pieces[rows[visualRank][file]]||'';sq.onclick=()=>selectBoardSquare(name);b.appendChild(sq);
  }
}
function perspectiveKey(){return $('listeningPerspective')?.value||'global';}
function entryPerception(entry){return entry.perception?.[perspectiveKey()]||null;}
function entryChord(entry){return entryPerception(entry)?.chord||entry.chord;}
function entryVoices(entry){const voices=entry.voicing?.[perspectiveKey()];return voices?.length?voices:entryChord(entry).midiNotes.map((m,i)=>({midiNote:m,detuneCents:0,velocity:.68,id:`summary-${i}`}));}
function candidateChord(candidate){return candidate.perception?.[perspectiveKey()]?.chord||candidate.chord;}
function render(){
  if(!game)return;const e=game.timeline[ply];renderBoard(e);$('plyLabel').textContent=ply===0?'Inicio':`Ply ${ply}/${game.timeline.length-1}`;$('moveTitle').textContent=ply===0?'Posición inicial':`${e.moveNumber}. ${e.san} · ${e.uci}`;
  const decision=e.decision; $('modeBadge').textContent=e.freeMove?'RAMA LIBRE':decision?.mode==='theory'?'TEORÍA · SIN BÚSQUEDA':decision?'Afinando…':(e.inTheory?'EN LIBRO':'POSICIÓN');
  $('opening').textContent=e.eco&&e.opening?`${e.eco} · ${e.opening}`:(decision?.eco&&decision?.opening?`${decision.eco} · ${decision.opening}`:'Sin identidad ECO/nombre para esta posición');
  const heard=entryPerception(e),heardChord=entryChord(e),voices=entryVoices(e);$('fen').textContent=e.fen;$('chord').textContent=heardChord.symbol;$('midi').textContent=`${voices.length} voces · ${[...new Set(voices.map(v=>midiName(v.midiNote)))].join(' · ')}`;$('tension').textContent=heard?.tension??0;$('energy').textContent=heard?.energy??e.harmony.tension;$('balance').textContent=heard?.score??e.harmony.relativeScore;
  renderComponents(e.harmony,perspectiveKey());renderDecision(e);renderTimeline();renderAudioGuide(heardChord,voices);if($('autoPlay').checked)playPosition(e);
}
function renderComponents(h,perspective){
  const host=$('components');host.innerHTML='';for(const [key,label] of Object.entries(labels)){const d=document.createElement('div');d.className='component';const value=perspective==='white'?h.white[key]:perspective==='black'?h.black[key]:`${h.white[key]} / ${h.black[key]}`;d.innerHTML=`<small>${label}</small><strong>${value}</strong>`;host.appendChild(d);}
}
function renderDecision(entry){
  const d=entry.decision;const text=$('decisionText'),host=$('candidateList');host.innerHTML='';if(!d){text.textContent='La posición inicial todavía no evalúa una jugada realizada.';return;}
  if(d.mode==='theory')text.textContent=`Jugada teórica #${d.rank}. Recomendación ${d.recommendedUci}: mayor armonía entre continuaciones teóricas; el peso del corpus desempata.`;
  else text.textContent=`${d.persona==='radioKiller'?'Radio Killer':'Piano Man'} · rango ${d.rank} · pérdida ${d.loss} · ${classEs[d.classification]||d.classification} · recomendación ${d.recommendedUci}.`;
  d.candidates.forEach((c,index)=>{
    const row=document.createElement('div');row.className='candidate'+(c.played?' played':'');const value=d.mode==='theory'?`peso ${c.weight}`:`score ${c.score}`;const loss=c.loss==null?'—':`−${c.loss}`;const heard=c.perception?.[perspectiveKey()],heardChord=candidateChord(c);
    row.innerHTML=`<strong>#${c.rank}${c.recommended?' ★':''}</strong><input class="voicePick" type="checkbox" data-index="${index}" ${index<10?'checked':''}><div><strong>${escapeHtml(c.san)}</strong> <code>${escapeHtml(c.uci)}</code></div><span class="secondary">${value}</span><span class="secondary">H ${c.harmonyScore}</span><span class="secondary">${loss}</span><span class="secondary">${escapeHtml(heardChord.symbol)}</span><span class="secondary">T ${heard?.tension??c.tension}</span><button class="listen">▶</button>`;
    row.querySelector('.listen').onclick=()=>playCandidate(entry,c,index,d.candidates.length);host.appendChild(row);
  });
}
function renderTimeline(){const t=$('timeline');t.innerHTML='';game.timeline.forEach((e,i)=>{const b=document.createElement('button');b.textContent=i===0?'•':e.san;b.title=e.fen;if(i===ply)b.classList.add('active');b.onclick=()=>{ply=i;selectedSquare=null;render();};t.appendChild(b);});t.children[ply]?.scrollIntoView({block:'nearest',inline:'nearest'});}

function midiHz(n){return 440*Math.pow(2,(n-69)/12);}
function midiName(n){return noteNames[((n%12)+12)%12]+(Math.floor(n/12)-1);}
function deterministicNoise(i,seed){const x=Math.sin((i+1)*(12.9898+seed*.001))*43758.5453;return (x-Math.floor(x))*2-1;}
function ensureAudio(){
  if(audio){if(audio.state==='suspended')audio.resume();return audio;}
  audio=new (window.AudioContext||window.webkitAudioContext)();
  const compressor=audio.createDynamicsCompressor();
  compressor.threshold.value=-18;compressor.knee.value=16;compressor.ratio.value=3;compressor.attack.value=.004;compressor.release.value=.28;
  audioMaster=audio.createGain();audioMaster.gain.value=.72;
  const dry=audio.createGain(),wet=audio.createGain(),room=audio.createConvolver();
  dry.gain.value=.9;wet.gain.value=.16;
  const seconds=1.15,length=Math.floor(audio.sampleRate*seconds),impulse=audio.createBuffer(2,length,audio.sampleRate);
  for(let ch=0;ch<2;ch++){const data=impulse.getChannelData(ch);for(let i=0;i<length;i++){const decay=Math.pow(1-i/length,2.8);data[i]=deterministicNoise(i,ch+37)*decay*.42;}}
  room.buffer=impulse;
  audioMaster.connect(dry).connect(compressor);
  audioMaster.connect(room).connect(wet).connect(compressor);
  compressor.connect(audio.destination);
  return audio;
}
function pianoNote(midi,when,duration,pan=0,velocity=.7,detune=0){
  const ctx=ensureAudio(),frequency=midiHz(midi),panner=ctx.createStereoPanner(),noteBus=ctx.createGain();
  panner.pan.value=Math.max(-1,Math.min(1,pan));noteBus.gain.value=1;noteBus.connect(panner).connect(audioMaster);
  const partials=[
    {ratio:1,level:1,life:1.35},
    {ratio:2.003,level:.38,life:.92},
    {ratio:3.012,level:.18,life:.62},
    {ratio:4.027,level:.085,life:.43},
    {ratio:5.045,level:.038,life:.30}
  ];
  partials.forEach((partial,index)=>{
    const osc=ctx.createOscillator(),g=ctx.createGain(),peak=.115*velocity*partial.level;
    osc.type='sine';osc.frequency.value=frequency*partial.ratio;osc.detune.value=detune+(index-1)*.35;
    g.gain.setValueAtTime(.0001,when);
    g.gain.exponentialRampToValueAtTime(Math.max(.0002,peak),when+.006+index*.0015);
    g.gain.exponentialRampToValueAtTime(Math.max(.0001,peak*.52),when+.075);
    g.gain.exponentialRampToValueAtTime(.0001,when+Math.max(.28,duration*partial.life));
    osc.connect(g).connect(noteBus);osc.start(when);osc.stop(when+Math.max(.38,duration*partial.life)+.08);
  });
  const hammerLength=Math.max(32,Math.floor(ctx.sampleRate*.018)),hammerBuffer=ctx.createBuffer(1,hammerLength,ctx.sampleRate),hammer=hammerBuffer.getChannelData(0);
  for(let i=0;i<hammerLength;i++)hammer[i]=deterministicNoise(i,midi+11)*Math.pow(1-i/hammerLength,4);
  const source=ctx.createBufferSource(),filter=ctx.createBiquadFilter(),hammerGain=ctx.createGain();
  source.buffer=hammerBuffer;filter.type='bandpass';filter.frequency.value=Math.min(5200,Math.max(900,frequency*5.5));filter.Q.value=.75;
  hammerGain.gain.setValueAtTime(.024*velocity,when);hammerGain.gain.exponentialRampToValueAtTime(.0001,when+.025);
  source.connect(filter).connect(hammerGain).connect(noteBus);source.start(when);source.stop(when+.03);
}
function playNotes(notes,start,duration,pan=0,gain=.055,arp=false){
  const ctx=ensureAudio(),now=ctx.currentTime+start,velocity=Math.max(.35,Math.min(.95,gain/.07));
  const step=Math.max(.11,Math.min(.24,duration*.28));
  notes.forEach((m,i)=>pianoNote(m,now+(arp?i*step:i*.004),duration+(arp ? .22 : 0),pan,velocity*(1-i*.025)));
}
function playVoices(voices,start,duration,pan=0,arp=false){
  const ctx=ensureAudio(),now=ctx.currentTime+start,step=Math.max(.065,Math.min(.14,duration*.08)),normalizer=Math.max(.28,Math.min(.72,4.8/Math.sqrt(Math.max(1,voices.length))));
  [...voices].sort((a,b)=>a.midiNote-b.midiNote||(a.square||'').localeCompare(b.square||'')).forEach((voice,i)=>pianoNote(voice.midiNote,now+(arp?i*step:i*.0025),duration+(arp ? .25 : 0),pan,(voice.velocity||.65)*normalizer,voice.detuneCents||0));
}
function renderAudioGuide(chord,voices){
  if(!chord||!$('audioGuide'))return;
  const names=voices.map(v=>midiName(v.midiNote)),mode=$('playMode').value,perspective={global:'Partida completa',white:'Percepción de Blancas',black:'Percepción de Negras'}[perspectiveKey()];
  if(mode==='arp'){
    $('audioGuide').innerHTML='<strong>'+escapeHtml(chord.symbol)+'</strong> · '+escapeHtml(perspective)+' · '+voices.length+' voces<br><span class="noteFlow">'+names.map(escapeHtml).join(' → ')+'</span><br><small>Arpegio: suena una vez cada pieza viva, de grave a agudo; al inicio son 32 notas.</small>';
  }else{
    $('audioGuide').innerHTML='<strong>'+escapeHtml(chord.symbol)+'</strong> · '+escapeHtml(perspective)+' · '+voices.length+' voces<br><span class="noteFlow">'+names.map(escapeHtml).join(' + ')+'</span><br><small>Acorde: cada pieza viva aporta su propia voz y todas suenan juntas.</small>';
  }
}
function playPosition(entry,pan=0,delay=0){const beat=60/Number($('tempo').value);playVoices(entryVoices(entry),delay,Math.max(.9,beat*1.8),pan,$('playMode').value==='arp');}
function playCandidate(entry,c,index,total){const pan=total<=1?0:-.78+1.56*(index/(total-1));const beat=60/Number($('tempo').value);playNotes(entryChord(entry).midiNotes,0,Math.max(.7,beat*1.25),pan,.048,$('playMode').value==='arp');playNotes(candidateChord(c).midiNotes,beat*1.05,Math.max(.85,beat*1.5),pan,.058,$('playMode').value==='arp');}
$('playPosition').onclick=()=>game&&playPosition(game.timeline[ply]);
$('compareBtn').onclick=()=>{if(!game)return;const e=game.timeline[ply],d=e.decision;if(!d)return;const picks=[...document.querySelectorAll('.voicePick:checked')].slice(0,10).map(x=>Number(x.dataset.index));picks.forEach((idx,i)=>playCandidate(e,d.candidates[idx],i,picks.length));};
$('playMode').onchange=()=>game&&renderAudioGuide(entryChord(game.timeline[ply]),entryVoices(game.timeline[ply]));
$('tempo').oninput=e=>$('tempoValue').textContent=e.target.value;

function selectBoardSquare(square){
  if(!game||moving)return;const entry=game.timeline[ply],legal=entry.legalMoves||[];
  if(selectedSquare){
    const matches=legal.filter(move=>move.from===selectedSquare&&move.to===square);
    if(matches.length){void makeFreeMove(choosePromotion(matches));return;}
  }
  selectedSquare=legal.some(move=>move.from===square)?square:null;renderBoard(entry);
}
function choosePromotion(matches){
  if(matches.length===1)return matches[0];
  const answer=(window.prompt('Promoción: Q, R, B o N','Q')||'Q').trim().toLowerCase();
  return matches.find(move=>move.uci.endsWith(answer))||matches.find(move=>move.promotion==='Queen')||matches[0];
}
async function makeFreeMove(move){
  moving=true;$('engineState').textContent='Afinando…';showError('');
  try{
    const current=game.timeline[ply],res=await fetch('/api/move',{method:'POST',headers:{'content-type':'application/json'},body:JSON.stringify({fen:current.fen,uci:move.uci,ply:ply+1})}),data=await res.json();
    if(!res.ok)throw new Error(data.error||'No se pudo analizar la jugada.');
    data.entry.freeMove=true;game.timeline=game.timeline.slice(0,ply+1);game.timeline.push(data.entry);game.finalFen=data.entry.fen;ply++;selectedSquare=null;marks=marks.filter(mark=>mark.ply<ply);$('resetBranch').hidden=false;render();
  }catch(err){showError(err.message);}finally{moving=false;$('engineState').textContent='Listo';}
}
function go(where){if(!game)return;const max=game.timeline.length-1;ply=where==='start'?0:where==='end'?max:where==='prev'?Math.max(0,ply-1):Math.min(max,ply+1);selectedSquare=null;render();}
document.querySelectorAll('[data-nav]').forEach(b=>b.onclick=()=>go(b.dataset.nav));
document.addEventListener('keydown',e=>{if(e.target.matches('textarea,input,select'))return;if(e.key==='ArrowLeft'){e.preventDefault();go('prev');}if(e.key==='ArrowRight'){e.preventDefault();go('next');}});
$('copyFen').onclick=async()=>{if(game)await navigator.clipboard.writeText(game.timeline[ply].fen);};
$('resetBranch').onclick=()=>selectGame(Number($('gameSelect').value||0));

$('markDissonance').onclick=()=>{
  if(!game||ply===0)return;const entry=game.timeline[ply];const observation={game:{title:game.title,event:game.event,white:game.white,black:game.black,result:game.result},ply,timestamp:new Date().toISOString(),san:entry.san,uci:entry.uci,fen:entry.fen,chord:entry.chord.symbol,midiNotes:entry.chord.midiNotes,voicing:entry.voicing,harmonicVector:{white:entry.harmony.white,black:entry.harmony.black,balance:entry.harmony.relativeScore},perception:entry.perception,perceptionDelta:entry.perceptionDelta,dissonance:entry.perception.global.tension,tacticalEnergy:entry.harmony.tension,opening:{eco:entry.eco,name:entry.opening,inTheory:entry.inTheory},decision:entry.decision};marks=marks.filter(x=>x.ply!==ply);marks.push(observation);$('markDissonance').textContent='✓ Disonancia marcada';setTimeout(()=>$('markDissonance').textContent='Marcar disonancia',1000);
};
$('exportMarks').onclick=()=>{
  if(!game)return;const exportData={format:'pianoman-auditory-observations-v2',exportedAt:new Date().toISOString(),game:{title:game.title,event:game.event,white:game.white,black:game.black,result:game.result},markedPlies:marks.map(x=>x.ply),observations:marks,timeline:game.timeline.map(e=>({ply:e.ply,san:e.san,uci:e.uci,fen:e.fen,chord:e.chord.symbol,midiNotes:e.chord.midiNotes,voicing:e.voicing,harmonicVector:{white:e.harmony.white,black:e.harmony.black,balance:e.harmony.relativeScore},perception:e.perception,perceptionDelta:e.perceptionDelta,dissonance:e.perception.global.tension,tacticalEnergy:e.harmony.tension,opening:{eco:e.eco,name:e.opening,inTheory:e.inTheory},decision:e.decision}))};
  const blob=new Blob([JSON.stringify(exportData,null,2)],{type:'application/json'});const a=document.createElement('a');a.href=URL.createObjectURL(blob);a.download='pianoman-dissonance-observations.json';a.click();setTimeout(()=>URL.revokeObjectURL(a.href),1000);
};
function showError(v){$('error').textContent=v;}
function escapeHtml(v){return String(v??'').replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));}
