using Microsoft.Dafny;
using Program = Microsoft.Dafny.Program;

namespace DafnyRepair.Fixes.Configuration;

public sealed class FixConfiguration
{
    public static FixConfiguration Instance;
    private static readonly HashSet<DebugInformation> ActiveDebug = new();
    public FixGoal Goal;
    public Program Program;

    private FixConfiguration(FixGoal goal, Program program)
    {
        this.Goal = goal;
        this.Program = program;
    }

    public static FixConfiguration CreateInstance(FixGoal goal, Program program)
    {
        Instance = new FixConfiguration(goal, program);
        return Instance;
    }

    public static FixConfiguration GetInstance()
    {
        return Instance;
    }

    public static FixGoal GetGoal()
    {
        return Instance.Goal;
    }

    public static Method GetOuter()
    {
        return Instance.Goal.Outer;
    }

    public static Method GetFaultyRoutine()
    {
        return Instance.Goal.FaultyRoutine;
    }

    public static ContractType GetBrokenContract()
    {
        return Instance.Goal.BrokenContract;
    }

    public static Program GetProgram()
    {
        return Instance.Program;
    }

    public static bool ShouldDebug(DebugInformation debugType)
    {
        return ActiveDebug.Contains(debugType);
    }
}
