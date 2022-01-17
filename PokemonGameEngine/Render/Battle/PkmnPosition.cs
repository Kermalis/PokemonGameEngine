using Kermalis.PokemonBattleEngine.Battle;
using Kermalis.PokemonGameEngine.Render.R3D;
using System;
using System.Numerics;

namespace Kermalis.PokemonGameEngine.Render.Battle
{
    internal sealed class PkmnPosition
    {
        public readonly PositionRotation FocusPreTurn;
        public readonly PositionRotation FocusTurn;
        public readonly Vector3 DefaultSpritePosition;
        private readonly Vec2I _barPos;

        public bool InfoVisible;
        public BattlePokemon BattlePkmn;
        public readonly BattleSprite Sprite;

        public PkmnPosition(float scale, in Vector3 defaultSpritePos, Vec2I barPos, in PositionRotation focusPreTurn, in PositionRotation focusTurn)
        {
            FocusPreTurn = focusPreTurn;
            FocusTurn = focusTurn;
            DefaultSpritePosition = defaultSpritePos;
            _barPos = barPos;

            Sprite = new BattleSprite(defaultSpritePos, false, scale: scale);
        }

        /// <summary>Assumes <see cref="BattlePokemon.DetachPos"/> was called first</summary>
        public void Clear()
        {
            InfoVisible = false;
            Sprite.IsVisible = false;
            Sprite.Image = null;
        }

        public void RenderMonInfo()
        {
            BattlePkmn.InfoBar.RenderColorTexture(_barPos);
        }

        public static PkmnPosition[][] CreatePositions(PBEBattleFormat f)
        {
            const int FOE_BAR_Y = 5;
            const int ALLY_BAR_Y = 55;
            const int BAR_X_START = 5;
            const int BAR_X_SPACING = 110;

            float floorY = BattleGUI.GetFloorY(f);
            var a = new PkmnPosition[2][]; // 0 is ally, 1 is foe
            switch (f)
            {
                case PBEBattleFormat.Single:
                {
                    a[0] = new PkmnPosition[1]
                    {
                        new(2f, // Center
                            new Vector3(-0.5f, floorY, 2f),
                            new Vec2I(BAR_X_START, ALLY_BAR_Y),
                            new PositionRotation(new Vector3(5.9f, 5.25f, 14.75f), new Rotation(-22f, 13f, 0f)),
                            new PositionRotation(new Vector3(6f, 6f, 17.3f), new Rotation(-22f, 13f, 0f)))
                    };
                    a[1] = new PkmnPosition[1]
                    {
                        new(1f, // Center
                            new Vector3(1f, floorY, -13.5f),
                            new Vec2I(BAR_X_START, FOE_BAR_Y),
                            default,
                            new PositionRotation(new Vector3(6.9f, 7f, 4.6f), new Rotation(-22f, 13f, 0f)))
                    };
                    break;
                }
                case PBEBattleFormat.Double:
                {
                    a[0] = new PkmnPosition[2]
                    {
                        new(2f, // Left
                            new Vector3(-4f, floorY, 2f),
                            new Vec2I(BAR_X_START, ALLY_BAR_Y),
                            new PositionRotation(new Vector3(3f, 5.2f, 14.8f), new Rotation(-22f, 13f, 0f)),
                            new PositionRotation(new Vector3(3f, 5.2f, 14.8f), new Rotation(-22f, 13f, 0f))),
                        new(2f, // Right
                            new Vector3(2.25f, floorY, 5f),
                            new Vec2I(BAR_X_START + BAR_X_SPACING, ALLY_BAR_Y),
                            new PositionRotation(new Vector3(7.6f, 5.5f, 15.85f), new Rotation(-20.35f, 13.25f, 0f)),
                            new PositionRotation(new Vector3(6.9f, 5.35f, 15.55f), new Rotation(-21.65f, 13f, 0f)))
                    };
                    a[1] = new PkmnPosition[2]
                    {
                        new(1f, // Left
                            new Vector3(3f, floorY, -13f),
                            new Vec2I(BAR_X_START, FOE_BAR_Y),
                            default,
                            new PositionRotation(new Vector3(9f, 7f, 5f), new Rotation(-22f, 13f, 0f))),
                        new(1f, // Right
                            new Vector3(-1.5f, floorY, -14f),
                            new Vec2I(BAR_X_START + BAR_X_SPACING, FOE_BAR_Y),
                            default,
                            new PositionRotation(new Vector3(5f, 7f, 5f), new Rotation(-22f, 13f, 0f)))
                    };
                    break;
                }
                case PBEBattleFormat.Triple:
                {
                    a[0] = new PkmnPosition[3]
                    {
                        new(2f, // Left
                            new Vector3(-3.5f, floorY, 5f),
                            new Vec2I(BAR_X_START, ALLY_BAR_Y),
                            new PositionRotation(new Vector3(-0.6f, 4f, 13.65f), new Rotation(-19.5f, 11f, 0f)),
                            new PositionRotation(new Vector3(2f, 6f, 18f), new Rotation(-21.75f, 12.5f, 0f))),
                        new(2f, // Center
                            new Vector3(1f, floorY, 2.5f),
                            new Vec2I(BAR_X_START + BAR_X_SPACING, ALLY_BAR_Y),
                            new PositionRotation(new Vector3(7.15f, 6f, 17.1f), new Rotation(-22f, 13.6f, 0f)),
                            new PositionRotation(new Vector3(6.8f, 6.55f, 18.8f), new Rotation(-22f, 13.4f, 0f))),
                        new(2f, // Right
                            new Vector3(6f, floorY, 4f),
                            new Vec2I(BAR_X_START + BAR_X_SPACING + BAR_X_SPACING, ALLY_BAR_Y),
                            new PositionRotation(new Vector3(9.9f, 5.4f, 15.5f), new Rotation(-21.65f, 13.2f, 0f)),
                            new PositionRotation(new Vector3(6.85f, 5.3f, 16.85f), new Rotation(-6f, 12.6f, 0f)))
                    };
                    a[1] = new PkmnPosition[3]
                    {
                        new(1f, // Left
                            new Vector3(4.5f, floorY, -14.5f),
                            new Vec2I(BAR_X_START, FOE_BAR_Y),
                            default,
                            new PositionRotation(new Vector3(12.15f, 7f, 4.75f), new Rotation(-22f, 13f, 0f))),
                        new(1f, // Center
                            new Vector3(0.75f, floorY, -11.5f),
                            new Vec2I(BAR_X_START + BAR_X_SPACING, FOE_BAR_Y),
                            default,
                            new PositionRotation(new Vector3(7f, 7f, 4.8f), new Rotation(-22f, 13f, 0f))),
                        new(1f, // Right
                            new Vector3(-4f, floorY, -14.5f),
                            new Vec2I(BAR_X_START + BAR_X_SPACING + BAR_X_SPACING, FOE_BAR_Y),
                            default,
                            new PositionRotation(new Vector3(3f, 7f, 4.8f), new Rotation(-22f, 13f, 0f)))
                    };
                    break;
                }
                case PBEBattleFormat.Rotation:
                {
                    // In rotation battles, the side positions are never really used.
                    // Statuses and even Perish Song do not affect them, so the foe's sides aren't able to be focused...
                    // At least in the preliminary testing I did. It's possible something like Helping Hand would move the camera over to them.
                    // The ally sides would also not be focused during the turn if there is no move to change the camera to the sides
                    a[0] = new PkmnPosition[3]
                    {
                        new(3f, // Left
                            new Vector3(-4.25f, floorY, 12f),
                            new Vec2I(BAR_X_START, ALLY_BAR_Y),
                            new PositionRotation(new Vector3(3.1f, 7f, 24.5f), new Rotation(-20.5f, 11.9f, 0f)),
                            default),
                        new(2f, // Center
                            new Vector3(-0.5f, floorY, 5f),
                            new Vec2I(BAR_X_START + BAR_X_SPACING, ALLY_BAR_Y),
                            new PositionRotation(new Vector3(8.3f, 7f, 23.75f), new Rotation(-21.5f, 12.1f, 0f)),
                            new PositionRotation(new Vector3(5.85f, 5.85f, 18.7f), new Rotation(-20.5f, 12.1f, 0f))),
                        new(4f, // Right
                            new Vector3(4.25f, floorY, 11.5f),
                            new Vec2I(BAR_X_START + BAR_X_SPACING + BAR_X_SPACING, ALLY_BAR_Y),
                            new PositionRotation(new Vector3(11f, 7.25f, 25f), new Rotation(-22f, 12f, 0f)),
                            default)
                    };
                    a[1] = new PkmnPosition[3]
                    {
                        new(1f, // Left
                            new Vector3(4f, floorY, -17f),
                            new Vec2I(BAR_X_START, FOE_BAR_Y),
                            default,
                            default),
                        new(1f, // Center
                            new Vector3(0f, floorY, -11f),
                            new Vec2I(BAR_X_START + BAR_X_SPACING, FOE_BAR_Y),
                            default,
                            new PositionRotation(new Vector3(7f, 7f, 4.5f), new Rotation(-22f, 13f, 0f))),
                        new(1f, // Right
                            new Vector3(-5f, floorY, -19f),
                            new Vec2I(BAR_X_START + BAR_X_SPACING + BAR_X_SPACING, FOE_BAR_Y),
                            default,
                            default)
                    };
                    break;
                }
                default: throw new ArgumentOutOfRangeException(nameof(f));
            }
            return a;
        }
    }
}
