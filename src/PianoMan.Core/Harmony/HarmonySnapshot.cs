using PianoMan.Core.Chess;
namespace PianoMan.Core.Harmony;
public sealed record HarmonySnapshot(SideHarmony White,SideHarmony Black,int RelativeScore,int Tension,int Phase,Color SideToMove,PositionStatus Status,Color? Winner)
{ public bool IsStable=>Tension<40; public int Balance=>RelativeScore; }
