// Bootstrapper for GH-AITranslator-Pro.
// Lives in the GHAITranslator (UI) assembly because it has to reference
// Grasshopper.Kernel.GH_AssemblyPriority directly. Core stays UI-clean so
// the test project can build it without referencing GH.

using System;
using System.IO;
using GH = Grasshopper;
using Grasshopper.Kernel;
using GHAITranslator.Core;

namespace GHAITranslator;

public class GHAITranslatorPlugin : GH_AssemblyPriority
{
    public override GH_LoadingInstruction PriorityLoad()
    {
        try
        {
            Bootstrapper.Initialize();
            return GH_LoadingInstruction.Proceed;
        }
        catch (Exception ex)
        {
            Bootstrapper.ShowFatal(ex);
            return GH_LoadingInstruction.Abort;
        }
    }
}