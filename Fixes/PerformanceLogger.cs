using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

namespace DafnyDriver.ContractChecking.Fixes;

public class PerformanceLogger {
  private readonly Dictionary<RuntimeActionType, List<long>> times;

  public PerformanceLogger() {
    times = new Dictionary<RuntimeActionType, List<long>>();
    foreach (RuntimeActionType type in Enum.GetValues(typeof(RuntimeActionType))) {
      times[type] = new List<long>();
    }
  }

  public void AddTime(RuntimeActionType type, long time) {
    times.TryGetValue(type, out var list);
    Debug.Assert(list != null, nameof(list) + " != null");
    list.Add(time);
  }

  private double GetAverageTime(RuntimeActionType type) {
    return times[type].Average();
  }
  
  public override string ToString() {
    var builder = new StringBuilder();
    builder.AppendLine("Tests");
    builder.AppendLine(GetAverageTime(RuntimeActionType.RUN_TESTS).ToString(CultureInfo.InvariantCulture));
    builder.AppendLine("Weakening");
    builder.AppendLine(GetAverageTime(RuntimeActionType.GENERATE_WEAKENING_DAIKON_INPUT).ToString(CultureInfo.InvariantCulture));
    builder.AppendLine(GetAverageTime(RuntimeActionType.RUN_WEAKENING_DAIKON).ToString(CultureInfo.InvariantCulture));
    builder.AppendLine(GetAverageTime(RuntimeActionType.PARSE_WEAKENING_DAIKON).ToString(CultureInfo.InvariantCulture));
    builder.AppendLine(GetAverageTime(RuntimeActionType.FILTER_WEAKENING_INVARIANTS).ToString(CultureInfo.InvariantCulture));
    builder.AppendLine(GetAverageTime(RuntimeActionType.CREATE_WEAKENING_FIXES).ToString(CultureInfo.InvariantCulture));
    builder.AppendLine(GetAverageTime(RuntimeActionType.VALIDATE_WEAKENING_FIXES).ToString(CultureInfo.InvariantCulture));
    builder.AppendLine("Strengthening");
    builder.AppendLine(GetAverageTime(RuntimeActionType.GENERATE_STRENGTHENING_DAIKON_INPUT).ToString(CultureInfo.InvariantCulture));
    builder.AppendLine(GetAverageTime(RuntimeActionType.RUN_STRENGTHENING_DAIKON).ToString(CultureInfo.InvariantCulture));
    builder.AppendLine(GetAverageTime(RuntimeActionType.PARSE_STRENGTHENING_DAIKON).ToString(CultureInfo.InvariantCulture));
    builder.AppendLine(GetAverageTime(RuntimeActionType.FILTER_STRENGTHENING_INVARIANTS).ToString(CultureInfo.InvariantCulture));
    builder.AppendLine(GetAverageTime(RuntimeActionType.CREATE_STRENGTHENING_FIXES).ToString(CultureInfo.InvariantCulture));
    builder.AppendLine(GetAverageTime(RuntimeActionType.VALIDATE_STRENGTHENING_FIXES).ToString(CultureInfo.InvariantCulture));
    builder.AppendLine("Total");
    builder.AppendLine(GetAverageTime(RuntimeActionType.ENTIRE_PROGRAM).ToString(CultureInfo.InvariantCulture));
    return builder.ToString();
  }
}

public enum RuntimeActionType {
  RUN_TESTS,
  GENERATE_WEAKENING_DAIKON_INPUT,
  RUN_WEAKENING_DAIKON,
  PARSE_WEAKENING_DAIKON,
  FILTER_WEAKENING_INVARIANTS,
  CREATE_WEAKENING_FIXES,
  VALIDATE_WEAKENING_FIXES,
  GENERATE_STRENGTHENING_DAIKON_INPUT,
  RUN_STRENGTHENING_DAIKON,
  PARSE_STRENGTHENING_DAIKON,
  FILTER_STRENGTHENING_INVARIANTS,
  CREATE_STRENGTHENING_FIXES,
  VALIDATE_STRENGTHENING_FIXES,
  ENTIRE_PROGRAM
}

public enum RuntimePhaseActionType {
  GENERATE_DAIKON_INPUT,
  RUN_DAIKON,
  PARSE_DAIKON,
  FILTER_INVARIANTS,
  CREATE_FIXES,
  VALIDATE_FIXES
}

public enum RuntimePhase {
  WEAKENING,
  STRENGTHENING
}