using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace WitchShmup.UI
{
    /// <summary>
    /// Fornece e gera os ícones pixel-art da Árvore de Habilidades (EvolutionUI)
    /// garantindo renderização idêntica ao esboço e sem dependência de fontes emoji do TMP.
    /// </summary>
    public static class SkillIconProvider
    {
        private static readonly Dictionary<string, Sprite> iconCache = new Dictionary<string, Sprite>();

        public static Sprite GetIcon(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            if (iconCache.TryGetValue(id, out var cached) && cached != null)
            {
                return cached;
            }

            // Tenta carregar dos Resources caso já exista como asset
            Sprite loaded = Resources.Load<Sprite>($"Sprites/SkillIcons/icon_{id}");
            if (loaded != null)
            {
                iconCache[id] = loaded;
                return loaded;
            }

            // Cria o sprite procedural pixel art em tempo de execução
            Sprite generated = GenerateIconSprite(id);
            if (generated != null)
            {
                iconCache[id] = generated;
            }

            return generated;
        }

        private static Sprite GenerateIconSprite(string id)
        {
            const int W = 24;
            const int H = 24;
            Texture2D tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color[] pixels = new Color[W * H];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;

            switch (id)
            {
                case "vit":
                    DrawHeart(pixels, W, H);
                    break;
                case "vel":
                    DrawLightning(pixels, W, H);
                    break;
                case "foc":
                    DrawSword(pixels, W, H);
                    break;
                case "des":
                    DrawStar(pixels, W, H);
                    break;
                case "pel":
                    DrawShield(pixels, W, H);
                    break;
                case "ima":
                    DrawMagnet(pixels, W, H);
                    break;
                case "seg":
                    DrawGhost(pixels, W, H);
                    break;
                case "rap":
                    DrawDoubleChevron(pixels, W, H);
                    break;
                case "per":
                    DrawPiercingArrow(pixels, W, H);
                    break;
                case "dup":
                    DrawMultiShotBullets(pixels, W, H);
                    break;
                default:
                    DrawStar(pixels, W, H);
                    break;
            }

            tex.SetPixels(pixels);
            tex.Apply();

            return Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), 16f);
        }

        private static void SetPixel(Color[] px, int w, int h, int x, int y, Color c)
        {
            if (x < 0 || x >= w || y < 0 || y >= h) return;
            px[y * w + x] = c;
        }

        // 1. Coração (Vitalidade)
        private static void DrawHeart(Color[] px, int w, int h)
        {
            Color cPink = new Color(1.00f, 0.36f, 0.56f, 1f);      // #ff5c8a
            Color cDark = new Color(0.24f, 0.06f, 0.15f, 1f);      // outline
            Color cShade = new Color(0.80f, 0.15f, 0.38f, 1f);     // sombra
            Color cWhite = Color.white;                            // brilho

            string[] map = {
                "........................",
                "........................",
                "........................",
                "........................",
                ".....XXXX......XXXX.....",
                "...XXPPPPXX..XXPPPPXX...",
                "..XPPWWPPPXXXXPPPPPPX...",
                "..XPWPPPPPPXXPPPPPPPX...",
                ".XPPPPPPPPPPPPPPPPPPPX..",
                ".XPPPPPPPPPPPPPPPPPPPX..",
                ".XPPPPPPPPPPPPPPPPPPPX..",
                ".XPPPPPPPPPPPPPPPPPSPX..",
                "..XPPPPPPPPPPPPPPPSSX...",
                "..XPPPPPPPPPPPPPPSSPX...",
                "...XPPPPPPPPPPPPSSPX....",
                "....XPPPPPPPPPPSSPX.....",
                ".....XPPPPPPPPSSPX......",
                "......XPPPPPPSSPX.......",
                ".......XPPPPSSPX........",
                "........XPPSSPX.........",
                ".........XSSPX..........",
                "..........XX............",
                "........................",
                "........................"
            };

            DrawMap(px, w, h, map, cDark, cPink, cWhite, cShade);
        }

        // 2. Raio (Vassoura Veloz)
        private static void DrawLightning(Color[] px, int w, int h)
        {
            Color cYellow = new Color(1.00f, 0.88f, 0.20f, 1f);
            Color cDark = new Color(0.28f, 0.18f, 0.02f, 1f);
            Color cWhite = new Color(1.00f, 0.98f, 0.85f, 1f);
            Color cOrange = new Color(0.96f, 0.55f, 0.05f, 1f);

            string[] map = {
                "........................",
                "........................",
                "..........XXXX..........",
                ".........XYYYYX.........",
                "........XYWWYYX.........",
                ".......XYWWYYX..........",
                "......XYWWYYX...........",
                ".....XYWWYYX............",
                "....XYWWYYYYYYYYXX......",
                "....XYYYYYYYYYYYYX......",
                "....XXXXXXXXXYYYX.......",
                "...........XYYYX........",
                "..........XYYYX.........",
                ".........XYWWYX.........",
                "........XYWWYX..........",
                ".......XYWWYX...........",
                "......XYYYYYX...........",
                "......XYYYYX............",
                ".....XYYYXX.............",
                ".....XYYX...............",
                "....XYYX................",
                "....XYX.................",
                "....XX..................",
                "........................"
            };

            DrawMap(px, w, h, map, cDark, cYellow, cWhite, cOrange);
        }

        // 3. Espada (Foco Arcano)
        private static void DrawSword(Color[] px, int w, int h)
        {
            Color cSilver = new Color(0.85f, 0.95f, 1.00f, 1f);
            Color cDark = new Color(0.12f, 0.15f, 0.20f, 1f);
            Color cCyan = new Color(0.25f, 0.85f, 1.00f, 1f);
            Color cGold = new Color(1.00f, 0.75f, 0.20f, 1f);

            string[] map = {
                ".....................XX.",
                "....................XYX.",
                "...................XYYX.",
                "..................XYWWX.",
                ".................XYWWYX.",
                "................XYWWYX..",
                "...............XYWWYX...",
                "..............XYWWYX....",
                ".............XYWWYX.....",
                "............XYWWYX......",
                "...........XYWWYX.......",
                "..........XYWWYX........",
                ".........XYWWYX.........",
                "....XX..XYWWYX..XX......",
                "...XGGXXYWWYX..XGGX.....",
                "..XGGGGXYWWYXXGGGGX.....",
                "..XGGGGGXYYYGGGGGGX.....",
                "...XXGGGGXXXXGGGGXX.....",
                ".....XXGXX..XXGXX.......",
                ".......XX....XX.........",
                "......XYX...............",
                ".....XYYX...............",
                "....XGGX................",
                ".....XX................."
            };

            DrawMap(px, w, h, map, cDark, cSilver, cCyan, cGold);
        }

        // 4. Estrela (Despertar Arcano)
        private static void DrawStar(Color[] px, int w, int h)
        {
            Color cGold = new Color(1.00f, 0.80f, 0.20f, 1f);
            Color cDark = new Color(0.28f, 0.18f, 0.05f, 1f);
            Color cWhite = Color.white;
            Color cOrange = new Color(1.00f, 0.55f, 0.10f, 1f);

            string[] map = {
                "........................",
                "...........XX...........",
                "...........XYX..........",
                "..........XYYX..........",
                "..........XYWYX.........",
                ".........XYWWYX.........",
                ".........XYWWYX.........",
                ".XXXXXXXXXYWWYXXXXXXXXX.",
                "..XYYYYYYYYWWYYYYYYYYX..",
                "...XYYYYYYYWWYYYYYYYX...",
                "....XYYYYYWWWWYYYYYX....",
                ".....XYYYWWWWWWYYYX.....",
                "......XYYWWWWWWYYX......",
                ".....XYYYWWWWWWYYYX.....",
                "....XYYYYYWWWWYYYYYX....",
                "...XYYYYYYYWWYYYYYYYX...",
                "..XYYYYYYYYWWYYYYYYYYX..",
                ".XXXXXXXXXYWWYXXXXXXXXX.",
                ".........XYWWYX.........",
                ".........XYWWYX.........",
                "..........XYWYX.........",
                "..........XYYX..........",
                "...........XYX..........",
                "...........XX..........."
            };

            DrawMap(px, w, h, map, cDark, cGold, cWhite, cOrange);
        }

        // 5. Escudo (Pele de Pedra)
        private static void DrawShield(Color[] px, int w, int h)
        {
            Color cCyan = new Color(0.35f, 0.85f, 1.00f, 1f);
            Color cDark = new Color(0.10f, 0.12f, 0.18f, 1f);
            Color cStone = new Color(0.45f, 0.52f, 0.62f, 1f);
            Color cSteel = new Color(0.70f, 0.78f, 0.88f, 1f);

            string[] map = {
                "........................",
                "........................",
                "...XXXXXXXXXXXXXXXXXX...",
                "..XCCCCCCCCCCCCCCCCCCX..",
                "..XCYSSSSSSSSSSSSSSYCX..",
                "..XCYSSSSSWWSSSSSSSYCX..",
                "..XCYSSSSSWWSSSSSSSYCX..",
                "..XCYSSSSSWWSSSSSSSYCX..",
                "..XCYSSSSSWWSSSSSSSYCX..",
                "..XCYSSWWWWWWWWSSSSYCX..",
                "..XCYSSWWWWWWWWSSSSYCX..",
                "..XCYSSSSSWWSSSSSSSYCX..",
                "...XCYSSSSWWSSSSSSYCX...",
                "...XCYSSSSWWSSSSSSYCX...",
                "....XCYSSSSSSSSSSYCX....",
                "....XCYSSSSSSSSSSYCX....",
                ".....XCYSSSSSSSSYCX.....",
                "......XCYSSSSSSYCX......",
                ".......XCYSSSSYCX.......",
                "........XCYSSYCX........",
                ".........XCYCYX.........",
                "..........XCCX..........",
                "...........XX...........",
                "........................"
            };

            DrawMap(px, w, h, map, cDark, cCyan, cSteel, cStone);
        }

        // 6. Ímã de Almas (Horseshoe Magnet)
        private static void DrawMagnet(Color[] px, int w, int h)
        {
            Color cRed = new Color(0.92f, 0.22f, 0.22f, 1f);
            Color cDark = new Color(0.18f, 0.05f, 0.05f, 1f);
            Color cSilver = new Color(0.88f, 0.92f, 0.96f, 1f);
            Color cShade = new Color(0.65f, 0.12f, 0.12f, 1f);

            string[] map = {
                "........................",
                "........................",
                ".......XXXXXXXXXX.......",
                ".....XXRRRRRRRRRRXX.....",
                "....XRRRRRRRRRRRRRRX....",
                "...XRRRRXXXXXXXXRRRRX...",
                "..XRRRRXX......XXRRRRX..",
                "..XRRRRX........XRRRRX..",
                ".XRRRRX..........XRRRRX.",
                ".XRRRRX..........XRRRRX.",
                ".XRRRRX..........XRRRRX.",
                ".XRRRRX..........XRRRRX.",
                ".XRRRRX..........XRRRRX.",
                ".XRRRRX..........XRRRRX.",
                ".XWWWWX..........XWWWWX.",
                ".XWWWWX..........XWWWWX.",
                ".XWWWWX..........XWWWWX.",
                ".XWWWWX..........XWWWWX.",
                ".XWWWWX..........XWWWWX.",
                "..XXXX............XXXX..",
                "........................",
                "........................",
                "........................",
                "........................"
            };

            DrawMap(px, w, h, map, cDark, cRed, cSilver, cShade);
        }

        // 7. Segundo Fôlego (Fantasma)
        private static void DrawGhost(Color[] px, int w, int h)
        {
            Color cWhite = new Color(0.94f, 0.92f, 0.98f, 1f);
            Color cDark = new Color(0.20f, 0.12f, 0.32f, 1f);
            Color cCyan = new Color(0.35f, 0.90f, 1.00f, 1f);
            Color cPurple = new Color(0.65f, 0.55f, 0.85f, 1f);

            string[] map = {
                "........................",
                ".........XXXXXX.........",
                ".......XXWWWWWWXX.......",
                "......XWWWWWWWWWWX......",
                ".....XWWWWWWWWWWWWX.....",
                "....XWWWWWWWWWWWWWWX....",
                "...XWWWWWWWWWWWWWWWWX...",
                "...XWWXXWWWWWWWWXXWWX...",
                "...XWXXXXWWWWWWXXXXWX...",
                "...XWXXXXWWWWWWXXXXWX...",
                "...XWWXXWWWWWWWWXXWWX...",
                "...XWWWWWWWWWWWWWWWWX...",
                "...XWWWWWWWWWWWWWWWWX...",
                "...XWWWWWWWWWWWWWWWWX...",
                "...XWWWWWWWWWWWWWWWWX...",
                "...XWWWWWWWWWWWWWWWWX...",
                "...XWWWWWWWWWWWWWWWWX...",
                "...XWWWWWWWWWWWWWWWWX...",
                "...XWWXWWXWWWWXWWXWWX...",
                "...XX.XWWX.XX.XWWX.XX...",
                ".......XX......XX.......",
                "........................",
                "........................",
                "........................"
            };

            DrawMap(px, w, h, map, cDark, cWhite, cCyan, cPurple);
        }

        // 8. Tiro Rápido (Double Chevron >>)
        private static void DrawDoubleChevron(Color[] px, int w, int h)
        {
            Color cCyan = new Color(0.15f, 0.92f, 1.00f, 1f);
            Color cDark = new Color(0.04f, 0.15f, 0.25f, 1f);
            Color cWhite = Color.white;
            Color cBlue = new Color(0.05f, 0.60f, 0.85f, 1f);

            string[] map = {
                "........................",
                "........................",
                "........................",
                "..XX..........XX........",
                "..XYX.........XYX.......",
                "..XYYX........XYYX......",
                "..XYWYX.......XYWYX.....",
                "..XYWWYX......XYWWYX....",
                "...XYWWYX......XYWWYX...",
                "....XYWWYX......XYWWYX..",
                ".....XYWWYX......XYWWYX.",
                "......XYWWX.......XYWWX.",
                ".....XYWWYX......XYWWYX.",
                "....XYWWYX......XYWWYX..",
                "...XYWWYX......XYWWYX...",
                "..XYWWYX......XYWWYX....",
                "..XYWYX.......XYWYX.....",
                "..XYYX........XYYX......",
                "..XYX.........XYX.......",
                "..XX..........XX........",
                "........................",
                "........................",
                "........................",
                "........................"
            };

            DrawMap(px, w, h, map, cDark, cCyan, cWhite, cBlue);
        }

        // 9. Tiro Perfurante (Flecha com rastro ->)
        private static void DrawPiercingArrow(Color[] px, int w, int h)
        {
            Color cOrange = new Color(1.00f, 0.45f, 0.05f, 1f);
            Color cDark = new Color(0.25f, 0.08f, 0.02f, 1f);
            Color cYellow = new Color(1.00f, 0.88f, 0.20f, 1f);
            Color cWhite = Color.white;

            string[] map = {
                "........................",
                "........................",
                "........................",
                "..................XX....",
                ".................XYX....",
                "................XYYX....",
                "...............XYWYX....",
                "..............XYWWYX....",
                ".............XYWWYX.....",
                "............XYWWYX......",
                ".XXXXXXXXXXXXXXXXWWWWX..",
                ".XYYYYYYYYYYYYYYYWWWWYX.",
                ".XYYYYYYYYYYYYYYYWWWWYX.",
                ".XXXXXXXXXXXXXXXXWWWWX..",
                "............XYWWYX......",
                ".............XYWWYX.....",
                "..............XYWWYX....",
                "...............XYWYX....",
                "................XYYX....",
                ".................XYX....",
                "..................XX....",
                "........................",
                "........................",
                "........................"
            };

            DrawMap(px, w, h, map, cDark, cOrange, cWhite, cYellow);
        }

        // 10. Fileiras de Disparo / Rajada Vertical (2 e 3 Projéteis Alinhados)
        private static void DrawMultiShotBullets(Color[] px, int w, int h)
        {
            Color cCyan = new Color(0.20f, 0.90f, 1.00f, 1f);
            Color cDark = new Color(0.05f, 0.15f, 0.28f, 1f);
            Color cWhite = Color.white;
            Color cBlue = new Color(0.10f, 0.60f, 0.90f, 1f);

            string[] map = {
                "........................",
                "....XXXX........XXXX....",
                "...XYYYYX......XYYYYX...",
                "..XYYYYYYX....XYYYYYYX..",
                "..XYWWWWYX....XYWWWWYX..",
                "..XYWWWWYX....XYWWWWYX..",
                "..XYWWWWYX....XYWWWWYX..",
                "..XYYYYYYX....XYYYYYYX..",
                "...XYYYYX......XYYYYX...",
                "....XXXX........XXXX....",
                "........................",
                "........................",
                "....XXXX........XXXX....",
                "...XYYYYX......XYYYYX...",
                "..XYYYYYYX....XYYYYYYX..",
                "..XYWWWWYX....XYWWWWYX..",
                "..XYWWWWYX....XYWWWWYX..",
                "..XYWWWWYX....XYWWWWYX..",
                "..XYYYYYYX....XYYYYYYX..",
                "...XYYYYX......XYYYYX...",
                "....XXXX........XXXX....",
                "........................",
                "........................",
                "........................"
            };

            DrawMap(px, w, h, map, cDark, cCyan, cWhite, cBlue);
        }

        private static void DrawMap(Color[] px, int w, int h, string[] map, Color cX, Color cY, Color cW, Color cS)
        {
            for (int r = 0; r < h; r++)
            {
                // Inverte Y para que a primeira linha da string seja o topo do sprite (Y=23)
                int y = h - 1 - r;
                if (r >= map.Length) break;
                string line = map[r];

                for (int x = 0; x < w; x++)
                {
                    if (x >= line.Length) break;
                    char ch = line[x];

                    switch (ch)
                    {
                        case 'X':
                            SetPixel(px, w, h, x, y, cX);
                            break;
                        case 'Y':
                        case 'P':
                        case 'R':
                        case 'C':
                            SetPixel(px, w, h, x, y, cY);
                            break;
                        case 'W':
                            SetPixel(px, w, h, x, y, cW);
                            break;
                        case 'S':
                        case 'G':
                            SetPixel(px, w, h, x, y, cS);
                            break;
                        default:
                            // '.' ou outro: transparente
                            break;
                    }
                }
            }
        }
    }
}
