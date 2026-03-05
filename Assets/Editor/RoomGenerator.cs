using UnityEditor;
using UnityEngine;

public static class RoomGenerator
{
    // Tile type codes
    const int Wr = 0;  // wall_reinforced
    const int Wp = 1;  // wall_plain_panel
    const int Wi = 2;  // wall_pipe
    const int Wk = 3;  // wall_cracked
    const int Wo = 4;  // wall_observation
    const int Dr = 5;  // interact_locked_door
    const int FC = 6;  // floor_clean_metal
    const int FD = 7;  // floor_dirty_metal
    const int FK = 8;  // floor_cracked
    const int FG = 9;  // floor_grated
    const int FH = 10; // floor_hazard

    static readonly string[] SpritePaths = new string[]
    {
        "Assets/Art/Sprites/Tiles/wall_reinforced.png",
        "Assets/Art/Sprites/Tiles/wall_plain_panel.png",
        "Assets/Art/Sprites/Tiles/wall_pipe.png",
        "Assets/Art/Sprites/Tiles/wall_cracked.png",
        "Assets/Art/Sprites/Tiles/wall_observation.png",
        "Assets/Art/Sprites/Interactables/interact_locked_door.png",
        "Assets/Art/Sprites/Tiles/floor_clean_metal.png",
        "Assets/Art/Sprites/Tiles/floor_dirty_metal.png",
        "Assets/Art/Sprites/Tiles/floor_cracked.png",
        "Assets/Art/Sprites/Tiles/floor_grated.png",
        "Assets/Art/Sprites/Tiles/floor_hazard.png",
    };

    // 17 rows (index 0=R01 bottom to index 16=R17 top), 28 columns each
    static readonly int[][] Grid = new int[][]
    {
        // R01 - bottom wall
        new[]{Wr,Wp,Wp,Wi,Wi,Wp,Wp,Wp,Wp,Wp,Wp,Wp,Wp,Dr,Dr,Wp,Wp,Wp,Wp,Wp,Wp,Wp,Wp,Wi,Wi,Wp,Wp,Wr},
        // R02
        new[]{Wp,FD,FD,FD,FD,FD,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,Wp},
        // R03
        new[]{Wp,FD,FD,FD,FD,FD,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FH,FH,FH,FH,FC,FC,FC,FC,Wp},
        // R04
        new[]{Wp,FD,FD,FD,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FH,FH,FH,FH,FC,FC,FC,FC,Wp},
        // R05
        new[]{Wp,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,Wp},
        // R06
        new[]{Wp,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FG,FG,FG,Wp},
        // R07
        new[]{Wp,FD,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FG,FG,FG,FG,Wi},
        // R08
        new[]{Wi,FD,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FG,FG,FG,FG,Wi},
        // R09
        new[]{Wp,FD,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FG,FG,FG,FG,Wi},
        // R10
        new[]{Wp,FD,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FG,FG,FG,FG,Wi},
        // R11
        new[]{Wi,FD,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FG,FG,FG,FG,Wi},
        // R12
        new[]{Wp,FK,FK,FK,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FG,FG,FG,FG,Wi},
        // R13
        new[]{Wk,FK,FK,FK,FK,FK,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FG,FG,FG,Wp},
        // R14
        new[]{Wk,FK,FK,FK,FK,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FG,FG,FG,Wp},
        // R15
        new[]{Wp,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,Wp},
        // R16
        new[]{Wp,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,FC,Wp},
        // R17 - top wall
        new[]{Wr,Wp,Wp,Wp,Wp,Wp,Wp,Wp,Wp,Wo,Wo,Wo,Wo,Wp,Wp,Wp,Wp,Wp,Wp,Wp,Wp,Wi,Wi,Wp,Wp,Wp,Wp,Wr},
    };

    struct OverlayDef
    {
        public string name;
        public string spritePath;
        public int col;
        public int row;
    }

    static readonly OverlayDef[] Props = new OverlayDef[]
    {
        new OverlayDef { name = "Console_Main",   spritePath = "Assets/Art/Sprites/Interactables/interact_console.png", col = 7,  row = 10 },
        new OverlayDef { name = "Console_Alt",     spritePath = "Assets/Art/Sprites/Interactables/interact_console.png", col = 15, row = 15 },
        new OverlayDef { name = "Terminal_1",      spritePath = "Assets/Art/Sprites/Props/prop_working_terminal.png",    col = 11, row = 15 },
        new OverlayDef { name = "Terminal_2",      spritePath = "Assets/Art/Sprites/Props/prop_working_terminal.png",    col = 19, row = 10 },
        new OverlayDef { name = "Monitor_Dead",    spritePath = "Assets/Art/Sprites/Props/prop_dead_monitor.png",        col = 3,  row = 13 },
        new OverlayDef { name = "Bench_1",         spritePath = "Assets/Art/Sprites/Props/prop_lab_bench.png",           col = 9,  row = 9  },
        new OverlayDef { name = "Bench_2",         spritePath = "Assets/Art/Sprites/Props/prop_lab_bench.png",           col = 16, row = 9  },
        new OverlayDef { name = "Canister_1",      spritePath = "Assets/Art/Sprites/Props/prop_specimen_canister.png",   col = 4,  row = 14 },
        new OverlayDef { name = "Canister_2",      spritePath = "Assets/Art/Sprites/Props/prop_specimen_canister.png",   col = 5,  row = 13 },
        new OverlayDef { name = "Crate_1",         spritePath = "Assets/Art/Sprites/Props/prop_crate_small.png",         col = 2,  row = 3  },
        new OverlayDef { name = "Crate_2",         spritePath = "Assets/Art/Sprites/Props/prop_crate_small.png",         col = 3,  row = 2  },
        new OverlayDef { name = "Crate_3",         spritePath = "Assets/Art/Sprites/Props/prop_crate_small.png",         col = 2,  row = 2  },
        new OverlayDef { name = "Cables",          spritePath = "Assets/Art/Sprites/Props/prop_cable_bundle.png",        col = 25, row = 9  },
        new OverlayDef { name = "Junction",        spritePath = "Assets/Art/Sprites/Props/prop_power_junction.png",      col = 25, row = 11 },
        new OverlayDef { name = "Warning_1",       spritePath = "Assets/Art/Sprites/Props/prop_warning_sign.png",        col = 19, row = 5  },
        new OverlayDef { name = "Warning_2",       spritePath = "Assets/Art/Sprites/Props/prop_warning_sign.png",        col = 1,  row = 11 },
        new OverlayDef { name = "Warning_3",       spritePath = "Assets/Art/Sprites/Props/prop_warning_sign.png",        col = 22, row = 4  },
    };

    static readonly OverlayDef[] Bio = new OverlayDef[]
    {
        new OverlayDef { name = "Node_Active",   spritePath = "Assets/Art/Sprites/Bio/bio_node_active.png",    col = 2,  row = 14 },
        new OverlayDef { name = "Node_Dormant",  spritePath = "Assets/Art/Sprites/Bio/bio_node_dormant.png",   col = 4,  row = 12 },
        new OverlayDef { name = "Moss_Glow_1",   spritePath = "Assets/Art/Sprites/Bio/bio_moss_glow.png",     col = 3,  row = 14 },
        new OverlayDef { name = "Moss_Glow_2",   spritePath = "Assets/Art/Sprites/Bio/bio_moss_glow.png",     col = 2,  row = 13 },
        new OverlayDef { name = "Moss_Med_1",    spritePath = "Assets/Art/Sprites/Bio/bio_moss_medium.png",   col = 1,  row = 14 },
        new OverlayDef { name = "Moss_Med_2",    spritePath = "Assets/Art/Sprites/Bio/bio_moss_medium.png",   col = 3,  row = 13 },
        new OverlayDef { name = "Moss_Sm_1",     spritePath = "Assets/Art/Sprites/Bio/bio_moss_small.png",    col = 1,  row = 15 },
        new OverlayDef { name = "Moss_Sm_2",     spritePath = "Assets/Art/Sprites/Bio/bio_moss_small.png",    col = 4,  row = 15 },
        new OverlayDef { name = "Moss_Sm_3",     spritePath = "Assets/Art/Sprites/Bio/bio_moss_small.png",    col = 5,  row = 12 },
        new OverlayDef { name = "Stem_1",        spritePath = "Assets/Art/Sprites/Bio/bio_stem_straight.png", col = 3,  row = 12 },
        new OverlayDef { name = "Stem_2",        spritePath = "Assets/Art/Sprites/Bio/bio_stem_bent.png",     col = 2,  row = 12 },
        new OverlayDef { name = "Tendril_1",     spritePath = "Assets/Art/Sprites/Bio/bio_tendril_tip.png",   col = 6,  row = 11 },
    };

    static Vector3 GridToWorld(int col, int row, float z = 0f)
    {
        return new Vector3(-3.375f + col * 0.25f, -2.125f + (row + 1) * 0.25f, z);
    }

    [MenuItem("Tools/Generate Reactor Access Lab")]
    static void Generate()
    {
        // Find or create parent
        GameObject parent = GameObject.Find("ReactorAccessLab");
        if (parent == null)
        {
            parent = new GameObject("ReactorAccessLab");
        }

        // Clear existing children (keep parent intact)
        while (parent.transform.childCount > 0)
        {
            Object.DestroyImmediate(parent.transform.GetChild(0).gameObject);
        }

        // Cache sprites
        var spriteCache = new Sprite[SpritePaths.Length];
        for (int i = 0; i < SpritePaths.Length; i++)
        {
            spriteCache[i] = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePaths[i]);
            if (spriteCache[i] == null)
                Debug.LogWarning($"[RoomGen] Sprite not found: {SpritePaths[i]}");
        }

        int tileCount = 0;

        // Create grid tiles
        for (int row = 0; row < Grid.Length; row++)
        {
            int[] rowData = Grid[row];
            for (int col = 0; col < rowData.Length; col++)
            {
                int tileType = rowData[col];
                Sprite sprite = spriteCache[tileType];
                if (sprite == null) continue;

                string prefix = tileType <= 5 ? "W" : "F";
                var go = new GameObject($"{prefix}_{col}_{row + 1}");
                go.transform.SetParent(parent.transform, false);
                go.transform.localPosition = GridToWorld(col, row);

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                tileCount++;
            }
        }

        // Create prop overlays
        for (int i = 0; i < Props.Length; i++)
        {
            CreateOverlay(parent.transform, Props[i]);
            tileCount++;
        }

        // Create bio overlays
        for (int i = 0; i < Bio.Length; i++)
        {
            CreateOverlay(parent.transform, Bio[i]);
            tileCount++;
        }

        EditorUtility.SetDirty(parent);
        Debug.Log($"[RoomGen] Created {tileCount} objects under ReactorAccessLab.");
    }

    static void CreateOverlay(Transform parent, OverlayDef def)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(def.spritePath);
        if (sprite == null)
        {
            Debug.LogWarning($"[RoomGen] Sprite not found: {def.spritePath}");
            return;
        }

        var go = new GameObject(def.name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = GridToWorld(def.col, def.row, -0.1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
    }
}
