using Mutagen.Bethesda;
using Mutagen.Bethesda.Fallout3;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Cache;
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
            foreach (var cell in state.LoadOrder.PriorityOrder.Cell().WinningContextOverrides(state.LinkCache))
            {
                if (cell is null || cell.Record is null)
                {
                    continue;
                }
                // aggregate Regions from winning override and any earlier instances of the CELL
                var cellLink = cell.Record.FormKey.ToLink<ICellGetter>();
                ISet<FormKey> regions = new HashSet<FormKey>();
                IList<IFormLinkGetter<IRegionGetter>> allRegions = new List<IFormLinkGetter<IRegionGetter>>();
                foreach (var cellVersion in cellLink.ResolveAll<ICellGetter>(state.LinkCache))
                {
                    if (cellVersion.Regions is null)
                    {
                        continue;
                    }
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
                }
            }
        }
    }
}
