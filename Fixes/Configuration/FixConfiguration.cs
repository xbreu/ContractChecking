using System.Collections.Generic;
using Microsoft.Dafny;

namespace DafnyDriver.ContractChecking.Fixes;

public sealed class FixConfiguration {
  public static FixConfiguration Instance;
  public FixGoal Goal;
  public Program Program;
  public static HashSet<DebugInformation> ActiveDebug = new HashSet<DebugInformation> {};

  private FixConfiguration(FixGoal Goal, Program Program) {
    this.Goal = Goal;
    this.Program = Program;
  }

  public static FixConfiguration CreateInstance(FixGoal Goal, Program Program) {
    Instance = new FixConfiguration(Goal, Program);
    return Instance;
  }

  public static FixConfiguration GetInstance() {
    return Instance;
  }

  public static FixGoal GetGoal() {
    return Instance.Goal;
  }
  
  public static Method GetOuter() {
    return Instance.Goal.Outer;
  }
  
  public static Method GetFaultyRoutine() {
    return Instance.Goal.FaultyRoutine;
  }
  
  public static ContractType GetBrokenContract() {
    return Instance.Goal.BrokenContract;
  }
  
  public static Program GetProgram() {
    return Instance.Program;
  }

  public static bool ShouldDebug(DebugInformation debugType) {
    return ActiveDebug.Contains(debugType);
  }
}