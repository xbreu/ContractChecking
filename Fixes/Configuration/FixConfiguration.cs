using System;
using System.Collections.Generic;
using Microsoft.Dafny;

namespace DafnyDriver.ContractChecking.Fixes;

public sealed class FixConfiguration {
  public static FixConfiguration Instance;
  private readonly DafnyOptions dafnyOptions;
  public FixGoal Goal;
  public Program Program;
  public readonly PerformanceLogger logger = new();
  private static readonly HashSet<DebugInformation> ActiveDebug = new() {
    // DebugInformation.PYTHON_EXECUTIONS,
    // DebugInformation.ACTION_RUNTIMES,
    DebugInformation.FIXES,
    // DebugInformation.INVARIANTS,
    // DebugInformation.EXECUTION_COUNTS
  };

  private FixConfiguration(FixGoal Goal, Program Program, DafnyOptions DafnyOptions) {
    this.Goal = Goal;
    this.Program = Program;
    this.dafnyOptions = DafnyOptions;
  }

  public static FixConfiguration CreateInstance(FixGoal Goal, Program Program, DafnyOptions DafnyOptions) {
    Instance = new FixConfiguration(Goal, Program, DafnyOptions);
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
  
  public static DafnyOptions GetOptions() {
    return Instance.dafnyOptions;
  }

  public static bool ShouldDebug(DebugInformation debugType) {
    return ActiveDebug.Contains(debugType);
  }
  
  public static void AddTime(RuntimeActionType type, long time) {
    Instance.logger.AddTime(type, time);
  }
  
  public static void AddTime(RuntimePhase phase, RuntimePhaseActionType type, long time) {
    var realType = (phase, type) switch {
      (RuntimePhase.WEAKENING, RuntimePhaseActionType.GENERATE_DAIKON_INPUT) => RuntimeActionType.GENERATE_WEAKENING_DAIKON_INPUT,
      (RuntimePhase.WEAKENING, RuntimePhaseActionType.RUN_DAIKON) => RuntimeActionType.RUN_WEAKENING_DAIKON,
      (RuntimePhase.WEAKENING, RuntimePhaseActionType.PARSE_DAIKON) => RuntimeActionType.PARSE_WEAKENING_DAIKON,
      (RuntimePhase.WEAKENING, RuntimePhaseActionType.FILTER_INVARIANTS) => RuntimeActionType.FILTER_WEAKENING_INVARIANTS,
      (RuntimePhase.WEAKENING, RuntimePhaseActionType.CREATE_FIXES) => RuntimeActionType.CREATE_WEAKENING_FIXES,
      (RuntimePhase.WEAKENING, RuntimePhaseActionType.VALIDATE_FIXES) => RuntimeActionType.VALIDATE_WEAKENING_FIXES,
      (RuntimePhase.STRENGTHENING, RuntimePhaseActionType.GENERATE_DAIKON_INPUT) => RuntimeActionType.GENERATE_STRENGTHENING_DAIKON_INPUT,
      (RuntimePhase.STRENGTHENING, RuntimePhaseActionType.RUN_DAIKON) => RuntimeActionType.RUN_STRENGTHENING_DAIKON,
      (RuntimePhase.STRENGTHENING, RuntimePhaseActionType.PARSE_DAIKON) => RuntimeActionType.PARSE_STRENGTHENING_DAIKON,
      (RuntimePhase.STRENGTHENING, RuntimePhaseActionType.FILTER_INVARIANTS) => RuntimeActionType.FILTER_STRENGTHENING_INVARIANTS,
      (RuntimePhase.STRENGTHENING, RuntimePhaseActionType.CREATE_FIXES) => RuntimeActionType.CREATE_STRENGTHENING_FIXES,
      (RuntimePhase.STRENGTHENING, RuntimePhaseActionType.VALIDATE_FIXES) => RuntimeActionType.VALIDATE_STRENGTHENING_FIXES
    };
    Instance.logger.AddTime(realType, time);
  }
}