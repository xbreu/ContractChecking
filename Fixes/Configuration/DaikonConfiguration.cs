using System.IO;
using System.Threading.Tasks;

namespace DafnyDriver.ContractChecking.Fixes;

public static class DaikonConfiguration {
  public static string SpinfoFile = @"PPT_NAME module_
pre_condition
  SIMPLIFY_FORMAT (EQ |pre_condition| |@true|)
!pre_condition
  SIMPLIFY_FORMAT (EQ |pre_condition| |@false|)
post_condition
  SIMPLIFY_FORMAT (EQ |post_condition| |@true|)
!post_condition
  SIMPLIFY_FORMAT (EQ |post_condition| |@false|)
passed_all_inside
  SIMPLIFY_FORMAT (EQ |passed_all_inside| |@true|)
!passed_all_inside
  SIMPLIFY_FORMAT (EQ |passed_all_inside| |@false|)";

  public static string ConfigFile = @"daikon.inv.Invariant.confidence_limit = 0
daikon.inv.Invariant.simplify_define_predicates = true
daikon.split.PptSplitter.dummy_invariant_level = 1
daikon.PptTopLevel.pairwise_implications = true
daikon.PrintInvariants.print_all = true";

  public static async Task WriteFiles() {
    await using (var writer = new StreamWriter("../test/.trace.spinfo")) {
      await writer.WriteAsync(SpinfoFile);
    }

    await using (var writer = new StreamWriter("../test/.trace.config")) {
      await writer.WriteAsync(ConfigFile);
    }
  }
}