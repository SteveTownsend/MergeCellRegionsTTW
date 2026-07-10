using Mutagen.Bethesda;
using Mutagen.Bethesda.Fallout3;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Synthesis;

namespace MergeCellRegionsTTW
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            return await SynthesisPipeline.Instance
                .AddPatch<IFallout3Mod, IFallout3ModGetter>(RunPatch)
                .SetTypicalOpen(GameRelease.Fallout3, "MergeRegionsTTW.esp")
                .Run(args);
        }

        public static void RunPatch(IPatcherState<IFallout3Mod, IFallout3ModGetter> state)
        {
            int cells = 0;
            int merged = 0;
            int needsCheck = 0;
            int cellOverrides = 0;
            int skipped = 0;
            Console.WriteLine("Aggregate all unique Region records from any master or override for an exterior CELL");
            foreach (var cell in state.LoadOrder.PriorityOrder.Cell().WinningContextOverrides(state.LinkCache))
            {
                ++cells;
                if (cell is null || cell.Record is null || cell.Record.Flags.HasFlag(Cell.Flag.IsInteriorCell))
                {
                    ++skipped;
                    continue;
                }
                ++needsCheck;
                // aggregate Regions from winning override and any earlier instances of the CELL
                var cellLink = cell.Record.FormKey.ToLink<ICellGetter>();
                ISet<FormKey> regions = new HashSet<FormKey>();
                IList<IFormLinkGetter<IRegionGetter>> allRegions = new List<IFormLinkGetter<IRegionGetter>>();
                foreach (var cellVersion in cellLink.ResolveAll(state.LinkCache))
                {
                    if (cellVersion.Regions is null)
                    {
                        continue;
                    }
                    ++cellOverrides;
                    foreach (var region in cellVersion.Regions)
                    {
                        if (regions.Add(region.FormKey))
                        {
                            allRegions.Add(region);
                        }
                    }
                }
                // Push Regions into a new override if the list got updated vs winning override
                if (regions.Count > 0 &&
                    (cell.Record.Regions is null || regions.Count > cell.Record.Regions.Count))
                {
                    var updated = cell.GetOrAddAsOverride(state.PatchMod);
                    updated.Regions = [.. allRegions];
                    ++merged;
                }
            }
            Console.WriteLine("Total CELLs {0}, skipped {1}, checked calls/overrides {2}/{3}, Regions merged {4}", cells, skipped, needsCheck, cellOverrides, merged);
        }
    }
}
