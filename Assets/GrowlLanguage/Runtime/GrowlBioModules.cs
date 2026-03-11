using System;
using System.Collections.Generic;
using GrowlLanguage.AST;

namespace GrowlLanguage.Runtime
{
    internal static class GrowlBioModules
    {
        internal static void Register(
            RuntimeEnvironment globals,
            Func<string, List<RuntimeArgument>, GrowlNode, object> invokeHost)
        {
            globals.Define("root", BuildModule("root", new[] {
                "grow_down", "grow_up", "grow_wide", "grow_toward",
                "branch", "thicken", "absorb", "absorb_all", "absorb_filtered",
                "set_absorption_rate", "deposit", "exude", "anchor", "connect_fungi",
                "sense_depth", "sense_moisture", "sense_obstacle", "sense_neighbors"
            }, invokeHost));

            globals.Define("stem", BuildModule("stem", new[] {
                "grow_up", "grow_horizontal", "grow_thick", "branch", "grow_segment",
                "split", "set_rigidity", "set_material", "store_water", "store_energy",
                "attach_to", "support_weight", "shed", "heal", "set_color",
                "set_texture", "produce_bark", "produce_wax"
            }, invokeHost));

            globals.Define("leaf", BuildModule("leaf", new[] {
                "grow", "grow_count", "reshape", "orient", "track_light",
                "set_angle_range", "open_stomata", "close_stomata",
                "set_stomata_schedule", "filter_gas", "set_color", "set_coating",
                "set_lifespan", "shed", "regrow", "absorb_moisture",
                "absorb_nutrients", "absorb_chemical"
            }, invokeHost));

            globals.Define("photo", BuildModule("photo", new[] {
                "process", "get_limiting_factor", "absorb_light", "set_pigment", "boost_chlorophyll",
                "set_light_saturation", "chemosynthesis", "thermosynthesis",
                "radiosynthesis", "parasitic", "decompose", "set_metabolism",
                "store_energy", "retrieve_energy", "share_energy"
            }, invokeHost));

            globals.Define("morph", BuildModule("morph", new[] {
                "create_part", "remove_part", "attach", "grow_part", "shrink_part",
                "set_symmetry", "set_growth_pattern", "set_surface", "emit_light",
                "orient_toward", "contract", "expand", "pulse"
            }, invokeHost));

            globals.Define("defense", BuildModule("defense", new[] {
                "grow_thorns", "grow_armor", "grow_camouflage", "produce_toxin",
                "produce_repellent", "produce_attractant", "sticky_trap",
                "resist_disease", "quarantine_part", "fever", "on_damage",
                "on_neighbor_distress"
            }, invokeHost));

            globals.Define("reproduce", BuildModule("reproduce", new[] {
                "generate_seeds", "set_dispersal", "set_germination", "mutate",
                "mutate_gene", "crossbreed", "clone", "fragment",
                "set_lifecycle", "set_maturity_age"
            }, invokeHost));

            // Global biological functions (not modules)
            RegisterGlobalFunc(globals, "synthesize", invokeHost);
            RegisterGlobalFunc(globals, "produce", invokeHost);
            RegisterGlobalFunc(globals, "emit", invokeHost);
        }

        private static Dictionary<object, object> BuildModule(
            string moduleName, string[] methods,
            Func<string, List<RuntimeArgument>, GrowlNode, object> invokeHost)
        {
            var dict = new Dictionary<object, object>(methods.Length);
            foreach (string method in methods)
            {
                string builtinName = moduleName + "_" + method;
                dict[method] = new RuntimeBuiltinFunction(
                    builtinName,
                    (interp, args, site) => invokeHost(builtinName, args, site));
            }
            return dict;
        }

        private static void RegisterGlobalFunc(
            RuntimeEnvironment globals, string name,
            Func<string, List<RuntimeArgument>, GrowlNode, object> invokeHost)
        {
            globals.Define(name, new RuntimeBuiltinFunction(
                name,
                (interp, args, site) => invokeHost(name, args, site)));
        }
    }
}
