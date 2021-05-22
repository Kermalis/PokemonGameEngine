using Kermalis.PokemonBattleEngine.Battle;
using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.Input;
using Kermalis.PokemonGameEngine.Render;
using System;

namespace Kermalis.PokemonGameEngine.GUI.Battle
{
    internal sealed class TargetsGUI
    {
        private enum TargetSelection : byte
        {
            FoeLeft,
            FoeCenter,
            FoeRight,
            AllyLeft,
            AllyCenter,
            AllyRight,
            Back
        }

        private sealed class TargetInfo
        {
            private readonly bool _ally;
            public bool Enabled;
            public SpritedBattlePokemon Pokemon;
            public bool LineRightVisible;
            public bool LineDownVisible;

            public PBETurnTarget Targets;

            public TargetInfo(bool ally)
            {
                _ally = ally;
            }

            public unsafe void Render(uint* bmpAddress, int bmpWidth, int bmpHeight, float x, float y, bool selected)
            {
                uint enabledC = _ally ? RenderUtils.Color(125, 100, 230, 255) : RenderUtils.Color(248, 80, 50, 255);
                RenderUtils.FillRectangle(bmpAddress, bmpWidth, bmpHeight, x, y, 0.24f, 0.2f, enabledC);

                if (Pokemon != null)
                {
                    Pokemon.Mini.DrawOn(bmpAddress, bmpWidth, bmpHeight, x, y + 0.025f);

                    Font.Default.DrawString(bmpAddress, bmpWidth, bmpHeight, x + 0.075f, y + 0.05f, Pokemon.Pkmn.KnownNickname, Font.DefaultWhite_I);
                }

                if (selected)
                {
                    uint selectedC = _ally ? RenderUtils.Color(75, 60, 150, 255) : RenderUtils.Color(120, 30, 10, 255);
                    RenderUtils.DrawThickRectangle(bmpAddress, bmpWidth, bmpHeight, x, y, 0.24f, 0.2f, 2, selectedC);
                }

                if (!Enabled)
                {
                    RenderUtils.FillRectangle(bmpAddress, bmpWidth, bmpHeight, x, y, 0.24f, 0.2f, RenderUtils.Color(0, 0, 0, 150));
                }
            }
        }

        private readonly TargetInfo _targetAllyLeft = new(true);
        private readonly TargetInfo _targetAllyCenter;
        private readonly TargetInfo _targetAllyRight = new(true);
        private readonly TargetInfo _targetFoeLeft = new(false);
        private readonly TargetInfo _targetFoeCenter;
        private readonly TargetInfo _targetFoeRight = new(false);

        private readonly bool _centerTargetsVisible;
        private readonly PBEBattlePokemon _pkmn;
        private readonly PBEMove _fightMove;
        private readonly Action _selectAction;
        private readonly Action _cancelAction;

        private int _selectionX;
        private int _selectionY;
        private TargetSelection _selection;

        public TargetsGUI(PBEBattlePokemon pkmn, PBEMoveTarget possibleTargets, PBEMove move, SpritedBattlePokemonParty[] spritedParties,
            Action selectAction, Action cancelAction)
        {
            SpritedBattlePokemon GetSprited(bool ally, PBEFieldPosition pos)
            {
                PBETeam deTeem = ally ? pkmn.Trainer.Team : pkmn.Trainer.Team.OpposingTeam;
                PBEBattlePokemon dePokeMone = deTeem.TryGetPokemon(pos);
                return dePokeMone is null ? null : spritedParties[dePokeMone.Trainer.Id][dePokeMone]; // Return null because it'll be disabled anyway
            }

            _pkmn = pkmn;
            _fightMove = move;
            _selectAction = selectAction;
            _cancelAction = cancelAction;
            _targetAllyLeft.Pokemon = GetSprited(true, PBEFieldPosition.Left);
            _targetAllyRight.Pokemon = GetSprited(true, PBEFieldPosition.Right);
            _targetFoeLeft.Pokemon = GetSprited(false, PBEFieldPosition.Left);
            _targetFoeRight.Pokemon = GetSprited(false, PBEFieldPosition.Right);

            if (pkmn.Battle.BattleFormat == PBEBattleFormat.Double)
            {
                _centerTargetsVisible = false;
                switch (possibleTargets)
                {
                    case PBEMoveTarget.All:
                    {
                        _selection = TargetSelection.FoeRight;
                        _targetAllyLeft.Enabled = _targetAllyRight.Enabled = _targetFoeLeft.Enabled = _targetFoeRight.Enabled = true;
                        _targetAllyLeft.LineRightVisible = _targetFoeRight.LineRightVisible = _targetFoeLeft.LineDownVisible = _targetFoeRight.LineDownVisible = true;
                        _targetAllyLeft.Targets = _targetAllyRight.Targets = _targetFoeLeft.Targets = _targetFoeRight.Targets = PBETurnTarget.AllyLeft | PBETurnTarget.AllyRight | PBETurnTarget.FoeLeft | PBETurnTarget.FoeRight;
                        break;
                    }
                    case PBEMoveTarget.AllFoes:
                    case PBEMoveTarget.AllFoesSurrounding:
                    {
                        _selection = TargetSelection.FoeRight;
                        _targetAllyLeft.Enabled = _targetAllyRight.Enabled = false;
                        _targetFoeLeft.Enabled = _targetFoeRight.Enabled = true;
                        _targetAllyLeft.LineRightVisible = _targetFoeLeft.LineDownVisible = _targetFoeRight.LineDownVisible = false;
                        _targetFoeRight.LineRightVisible = true;
                        _targetFoeLeft.Targets = _targetFoeRight.Targets = PBETurnTarget.FoeLeft | PBETurnTarget.FoeRight;
                        break;
                    }
                    case PBEMoveTarget.AllSurrounding:
                    {
                        _selection = TargetSelection.FoeRight;
                        if (pkmn.FieldPosition == PBEFieldPosition.Left)
                        {
                            _targetAllyLeft.Enabled = false;
                            _targetAllyRight.Enabled = true;
                            _targetFoeRight.LineDownVisible = false;
                            _targetFoeLeft.LineDownVisible = true;
                            _targetAllyRight.Targets = _targetFoeLeft.Targets = _targetFoeRight.Targets = PBETurnTarget.AllyRight | PBETurnTarget.FoeLeft | PBETurnTarget.FoeRight;
                        }
                        else
                        {
                            _targetAllyLeft.Enabled = true;
                            _targetAllyRight.Enabled = false;
                            _targetFoeRight.LineDownVisible = true;
                            _targetFoeLeft.LineDownVisible = false;
                            _targetAllyLeft.Targets = _targetFoeLeft.Targets = _targetFoeRight.Targets = PBETurnTarget.AllyLeft | PBETurnTarget.FoeLeft | PBETurnTarget.FoeRight;
                        }
                        _targetFoeLeft.Enabled = _targetFoeRight.Enabled = true;
                        _targetAllyLeft.LineRightVisible = false;
                        _targetFoeRight.LineRightVisible = true;
                        break;
                    }
                    case PBEMoveTarget.AllTeam:
                    {
                        if (_pkmn.FieldPosition == PBEFieldPosition.Left)
                        {
                            _selection = TargetSelection.AllyLeft;
                        }
                        else
                        {
                            _selection = TargetSelection.AllyRight;
                        }
                        _targetAllyLeft.Enabled = _targetAllyRight.Enabled = true;
                        _targetFoeLeft.Enabled = _targetFoeRight.Enabled = false;
                        _targetFoeRight.LineRightVisible = _targetFoeLeft.LineDownVisible = _targetFoeRight.LineDownVisible = false;
                        _targetAllyLeft.LineRightVisible = true;
                        _targetAllyLeft.Targets = _targetAllyRight.Targets = PBETurnTarget.AllyLeft | PBETurnTarget.AllyRight;
                        break;
                    }
                    case PBEMoveTarget.RandomFoeSurrounding:
                    case PBEMoveTarget.Self:
                    {
                        if (pkmn.FieldPosition == PBEFieldPosition.Left)
                        {
                            _selection = TargetSelection.AllyLeft;
                            _targetAllyLeft.Enabled = true;
                            _targetAllyRight.Enabled = false;
                            _targetAllyLeft.Targets = PBETurnTarget.AllyLeft;
                        }
                        else
                        {
                            _selection = TargetSelection.AllyRight;
                            _targetAllyLeft.Enabled = false;
                            _targetAllyRight.Enabled = true;
                            _targetAllyRight.Targets = PBETurnTarget.AllyRight;
                        }
                        _targetFoeLeft.Enabled = _targetFoeRight.Enabled = false;
                        _targetAllyLeft.LineRightVisible = _targetFoeRight.LineRightVisible = _targetFoeLeft.LineDownVisible = _targetFoeRight.LineDownVisible = false;
                        break;
                    }
                    case PBEMoveTarget.SelfOrAllySurrounding:
                    {
                        if (pkmn.FieldPosition == PBEFieldPosition.Left)
                        {
                            _selection = TargetSelection.AllyLeft;
                        }
                        else
                        {
                            _selection = TargetSelection.AllyRight;
                        }
                        _targetAllyLeft.Enabled = _targetAllyRight.Enabled = true;
                        _targetFoeLeft.Enabled = _targetFoeRight.Enabled = false;
                        _targetAllyLeft.LineRightVisible = _targetFoeRight.LineRightVisible = _targetFoeLeft.LineDownVisible = _targetFoeRight.LineDownVisible = false;
                        _targetAllyLeft.Targets = PBETurnTarget.AllyLeft;
                        _targetAllyRight.Targets = PBETurnTarget.AllyRight;
                        break;
                    }
                    case PBEMoveTarget.SingleAllySurrounding:
                    {
                        if (pkmn.FieldPosition == PBEFieldPosition.Left)
                        {
                            _selection = TargetSelection.AllyRight;
                            _targetAllyLeft.Enabled = false;
                            _targetAllyRight.Enabled = true;
                            _targetAllyRight.Targets = PBETurnTarget.AllyRight;
                        }
                        else
                        {
                            _selection = TargetSelection.AllyLeft;
                            _targetAllyLeft.Enabled = true;
                            _targetAllyRight.Enabled = false;
                            _targetAllyLeft.Targets = PBETurnTarget.AllyLeft;
                        }
                        _targetFoeLeft.Enabled = _targetFoeRight.Enabled = false;
                        _targetAllyLeft.LineRightVisible = _targetFoeRight.LineRightVisible = _targetFoeLeft.LineDownVisible = _targetFoeRight.LineDownVisible = false;
                        break;
                    }
                    case PBEMoveTarget.SingleFoeSurrounding:
                    {
                        _selection = TargetSelection.FoeRight;
                        _targetAllyLeft.Enabled = _targetAllyRight.Enabled = false;
                        _targetFoeLeft.Enabled = _targetFoeRight.Enabled = true;
                        _targetAllyLeft.LineRightVisible = _targetFoeRight.LineRightVisible = _targetFoeLeft.LineDownVisible = _targetFoeRight.LineDownVisible = false;
                        _targetFoeLeft.Targets = PBETurnTarget.FoeLeft;
                        _targetFoeRight.Targets = PBETurnTarget.FoeRight;
                        break;
                    }
                    case PBEMoveTarget.SingleNotSelf:
                    case PBEMoveTarget.SingleSurrounding:
                    {
                        _selection = TargetSelection.FoeRight;
                        if (pkmn.FieldPosition == PBEFieldPosition.Left)
                        {
                            _targetAllyLeft.Enabled = false;
                            _targetAllyRight.Enabled = true;
                            _targetAllyRight.Targets = PBETurnTarget.AllyRight;
                        }
                        else
                        {
                            _targetAllyLeft.Enabled = true;
                            _targetAllyRight.Enabled = false;
                            _targetAllyLeft.Targets = PBETurnTarget.AllyLeft;
                        }
                        _targetFoeLeft.Enabled = _targetFoeRight.Enabled = true;
                        _targetAllyLeft.LineRightVisible = _targetFoeRight.LineRightVisible = _targetFoeLeft.LineDownVisible = _targetFoeRight.LineDownVisible = false;
                        _targetFoeLeft.Targets = PBETurnTarget.FoeLeft;
                        _targetFoeRight.Targets = PBETurnTarget.FoeRight;
                        break;
                    }
                    default: throw new ArgumentOutOfRangeException(nameof(possibleTargets));
                }
            }
            else // Triple
            {
                _targetAllyCenter = new TargetInfo(true) { Pokemon = GetSprited(true, PBEFieldPosition.Center) };
                _targetFoeCenter = new TargetInfo(false) { Pokemon = GetSprited(false, PBEFieldPosition.Center) };
                _centerTargetsVisible = true;
                switch (possibleTargets)
                {
                    case PBEMoveTarget.All:
                    {
                        _selection = TargetSelection.FoeRight;
                        _targetAllyLeft.Enabled = _targetAllyCenter.Enabled = _targetAllyRight.Enabled = _targetFoeLeft.Enabled = _targetFoeCenter.Enabled = _targetFoeRight.Enabled = true;
                        _targetAllyLeft.LineRightVisible = _targetAllyCenter.LineRightVisible = _targetFoeRight.LineRightVisible = _targetFoeCenter.LineRightVisible = _targetFoeCenter.LineDownVisible = _targetFoeLeft.LineDownVisible = _targetFoeRight.LineDownVisible = true;
                        _targetAllyLeft.Targets = _targetAllyCenter.Targets = _targetAllyRight.Targets = _targetFoeLeft.Targets = _targetFoeCenter.Targets = _targetFoeRight.Targets = PBETurnTarget.AllyLeft | PBETurnTarget.AllyCenter | PBETurnTarget.AllyRight | PBETurnTarget.FoeLeft | PBETurnTarget.FoeCenter | PBETurnTarget.FoeRight;
                        break;
                    }
                    case PBEMoveTarget.AllFoes:
                    {
                        _selection = TargetSelection.FoeRight;
                        _targetAllyLeft.Enabled = _targetAllyCenter.Enabled = _targetAllyRight.Enabled = false;
                        _targetFoeLeft.Enabled = _targetFoeCenter.Enabled = _targetFoeRight.Enabled = true;
                        _targetAllyLeft.LineRightVisible = _targetAllyCenter.LineRightVisible = _targetFoeCenter.LineDownVisible = _targetFoeLeft.LineDownVisible = _targetFoeRight.LineDownVisible = false;
                        _targetFoeRight.LineRightVisible = _targetFoeCenter.LineRightVisible = true;
                        _targetFoeLeft.Targets = _targetFoeCenter.Targets = _targetFoeRight.Targets = PBETurnTarget.FoeLeft | PBETurnTarget.FoeCenter | PBETurnTarget.FoeRight;
                        break;
                    }
                    case PBEMoveTarget.AllFoesSurrounding:
                    {
                        if (pkmn.FieldPosition == PBEFieldPosition.Left)
                        {
                            _selection = TargetSelection.FoeRight;
                            _targetFoeCenter.Enabled = _targetFoeRight.Enabled = true;
                            _targetFoeLeft.Enabled = false;
                            _targetFoeRight.LineRightVisible = true;
                            _targetFoeCenter.LineRightVisible = false;
                            _targetFoeCenter.Targets = _targetFoeRight.Targets = PBETurnTarget.FoeCenter | PBETurnTarget.FoeRight;
                        }
                        else if (pkmn.FieldPosition == PBEFieldPosition.Center)
                        {
                            _selection = TargetSelection.FoeRight;
                            _targetFoeLeft.Enabled = _targetFoeCenter.Enabled = _targetFoeRight.Enabled = true;
                            _targetFoeRight.LineRightVisible = _targetFoeCenter.LineRightVisible = true;
                            _targetFoeLeft.Targets = _targetFoeCenter.Targets = _targetFoeRight.Targets = PBETurnTarget.FoeLeft | PBETurnTarget.FoeCenter | PBETurnTarget.FoeRight;
                        }
                        else
                        {
                            _selection = TargetSelection.FoeCenter;
                            _targetFoeLeft.Enabled = _targetFoeCenter.Enabled = true;
                            _targetFoeRight.Enabled = false;
                            _targetFoeCenter.LineRightVisible = true;
                            _targetFoeRight.LineRightVisible = false;
                            _targetFoeLeft.Targets = _targetFoeCenter.Targets = PBETurnTarget.FoeLeft | PBETurnTarget.FoeCenter;
                        }
                        _targetAllyLeft.Enabled = _targetAllyCenter.Enabled = _targetAllyRight.Enabled = false;
                        _targetFoeRight.LineDownVisible = _targetAllyLeft.LineRightVisible = _targetAllyCenter.LineRightVisible = _targetFoeCenter.LineDownVisible = _targetFoeLeft.LineDownVisible = false;
                        break;
                    }
                    case PBEMoveTarget.AllSurrounding:
                    {
                        if (pkmn.FieldPosition == PBEFieldPosition.Left)
                        {
                            _selection = TargetSelection.FoeRight;
                            _targetFoeRight.Enabled = _targetFoeCenter.Enabled = _targetAllyCenter.Enabled = true;
                            _targetAllyLeft.Enabled = _targetAllyRight.Enabled = _targetFoeLeft.Enabled = false;
                            _targetAllyLeft.LineRightVisible = _targetAllyCenter.LineRightVisible = _targetFoeCenter.LineRightVisible = _targetFoeLeft.LineDownVisible = _targetFoeRight.LineDownVisible = false;
                            _targetFoeRight.LineRightVisible = _targetFoeCenter.LineDownVisible = true;
                            _targetAllyCenter.Targets = _targetFoeCenter.Targets = _targetFoeRight.Targets = PBETurnTarget.AllyCenter | PBETurnTarget.FoeCenter | PBETurnTarget.FoeRight;
                        }
                        else if (pkmn.FieldPosition == PBEFieldPosition.Center)
                        {
                            _selection = TargetSelection.FoeRight;
                            _targetAllyLeft.Enabled = _targetAllyRight.Enabled = _targetFoeLeft.Enabled = _targetFoeCenter.Enabled = _targetFoeRight.Enabled = true;
                            _targetAllyCenter.Enabled = false;
                            _targetAllyLeft.LineRightVisible = _targetAllyCenter.LineRightVisible = _targetFoeCenter.LineDownVisible = false;
                            _targetFoeRight.LineRightVisible = _targetFoeCenter.LineRightVisible = _targetFoeLeft.LineDownVisible = _targetFoeRight.LineDownVisible = true;
                            _targetAllyLeft.Targets = _targetAllyRight.Targets = _targetFoeLeft.Targets = _targetFoeCenter.Targets = _targetFoeRight.Targets = PBETurnTarget.AllyLeft | PBETurnTarget.AllyRight | PBETurnTarget.FoeLeft | PBETurnTarget.FoeCenter | PBETurnTarget.FoeRight;
                        }
                        else
                        {
                            _selection = TargetSelection.FoeCenter;
                            _targetAllyLeft.Enabled = _targetAllyRight.Enabled = _targetFoeRight.Enabled = false;
                            _targetAllyCenter.Enabled = _targetFoeLeft.Enabled = _targetFoeCenter.Enabled = true;
                            _targetAllyLeft.LineRightVisible = _targetAllyCenter.LineRightVisible = _targetFoeRight.LineRightVisible = _targetFoeLeft.LineDownVisible = _targetFoeRight.LineDownVisible = false;
                            _targetFoeCenter.LineRightVisible = _targetFoeCenter.LineDownVisible = true;
                            _targetAllyCenter.Targets = _targetFoeLeft.Targets = _targetFoeCenter.Targets = PBETurnTarget.AllyCenter | PBETurnTarget.FoeLeft | PBETurnTarget.FoeCenter;
                        }
                        break;
                    }
                    case PBEMoveTarget.AllTeam:
                    {
                        if (pkmn.FieldPosition == PBEFieldPosition.Left)
                        {
                            _selection = TargetSelection.AllyLeft;
                        }
                        else if (pkmn.FieldPosition == PBEFieldPosition.Center)
                        {
                            _selection = TargetSelection.AllyCenter;
                        }
                        else
                        {
                            _selection = TargetSelection.AllyRight;
                        }
                        _targetAllyLeft.Enabled = _targetAllyCenter.Enabled = _targetAllyRight.Enabled = true;
                        _targetFoeLeft.Enabled = _targetFoeCenter.Enabled = _targetFoeRight.Enabled = false;
                        _targetAllyLeft.LineRightVisible = _targetAllyCenter.LineRightVisible = true;
                        _targetFoeRight.LineRightVisible = _targetFoeCenter.LineRightVisible = _targetFoeCenter.LineDownVisible = _targetFoeLeft.LineDownVisible = _targetFoeRight.LineDownVisible = false;
                        _targetAllyLeft.Targets = _targetAllyCenter.Targets = _targetAllyRight.Targets = PBETurnTarget.AllyLeft | PBETurnTarget.AllyCenter | PBETurnTarget.AllyRight;
                        break;
                    }
                    case PBEMoveTarget.RandomFoeSurrounding:
                    case PBEMoveTarget.Self:
                    {
                        if (pkmn.FieldPosition == PBEFieldPosition.Left)
                        {
                            _selection = TargetSelection.AllyLeft;
                            _targetAllyLeft.Enabled = true;
                            _targetAllyCenter.Enabled = _targetAllyRight.Enabled = false;
                            _targetAllyLeft.Targets = PBETurnTarget.AllyLeft;
                        }
                        else if (pkmn.FieldPosition == PBEFieldPosition.Center)
                        {
                            _selection = TargetSelection.AllyCenter;
                            _targetAllyCenter.Enabled = true;
                            _targetAllyLeft.Enabled = _targetAllyRight.Enabled = false;
                            _targetAllyCenter.Targets = PBETurnTarget.AllyCenter;
                        }
                        else
                        {
                            _selection = TargetSelection.AllyRight;
                            _targetAllyRight.Enabled = true;
                            _targetAllyLeft.Enabled = _targetAllyCenter.Enabled = false;
                            _targetAllyRight.Targets = PBETurnTarget.AllyRight;
                        }
                        _targetFoeLeft.Enabled = _targetFoeCenter.Enabled = _targetFoeRight.Enabled = false;
                        _targetAllyLeft.LineRightVisible = _targetAllyCenter.LineRightVisible = _targetFoeRight.LineRightVisible = _targetFoeCenter.LineRightVisible = _targetFoeCenter.LineDownVisible = _targetFoeLeft.LineDownVisible = _targetFoeRight.LineDownVisible = false;
                        break;
                    }
                    case PBEMoveTarget.SelfOrAllySurrounding:
                    {
                        if (pkmn.FieldPosition == PBEFieldPosition.Left)
                        {
                            _selection = TargetSelection.AllyLeft;
                            _targetAllyLeft.Enabled = _targetAllyCenter.Enabled = true;
                            _targetAllyRight.Enabled = false;
                            _targetAllyLeft.Targets = PBETurnTarget.AllyLeft;
                            _targetAllyCenter.Targets = PBETurnTarget.AllyCenter;
                        }
                        else if (pkmn.FieldPosition == PBEFieldPosition.Center)
                        {
                            _selection = TargetSelection.AllyCenter;
                            _targetAllyCenter.Enabled = _targetAllyLeft.Enabled = _targetAllyRight.Enabled = true;
                            _targetAllyLeft.Targets = PBETurnTarget.AllyLeft;
                            _targetAllyCenter.Targets = PBETurnTarget.AllyCenter;
                            _targetAllyRight.Targets = PBETurnTarget.AllyRight;
                        }
                        else
                        {
                            _selection = TargetSelection.AllyRight;
                            _targetAllyCenter.Enabled = _targetAllyRight.Enabled = true;
                            _targetAllyLeft.Enabled = false;
                            _targetAllyCenter.Targets = PBETurnTarget.AllyCenter;
                            _targetAllyRight.Targets = PBETurnTarget.AllyRight;
                        }
                        _targetFoeLeft.Enabled = _targetFoeCenter.Enabled = _targetFoeRight.Enabled = false;
                        _targetAllyLeft.LineRightVisible = _targetAllyCenter.LineRightVisible = _targetFoeRight.LineRightVisible = _targetFoeCenter.LineRightVisible = _targetFoeCenter.LineDownVisible = _targetFoeLeft.LineDownVisible = _targetFoeRight.LineDownVisible = false;
                        break;
                    }
                    case PBEMoveTarget.SingleAllySurrounding:
                    {
                        if (pkmn.FieldPosition == PBEFieldPosition.Left || pkmn.FieldPosition == PBEFieldPosition.Right)
                        {
                            _selection = TargetSelection.AllyCenter;
                            _targetAllyCenter.Enabled = true;
                            _targetAllyLeft.Enabled = _targetAllyRight.Enabled = false;
                            _targetAllyCenter.Targets = PBETurnTarget.AllyCenter;
                        }
                        else
                        {
                            _selection = TargetSelection.AllyLeft;
                            _targetAllyCenter.Enabled = false;
                            _targetAllyLeft.Enabled = _targetAllyRight.Enabled = true;
                            _targetAllyLeft.Targets = PBETurnTarget.AllyLeft;
                            _targetAllyRight.Targets = PBETurnTarget.AllyRight;
                        }
                        _targetFoeLeft.Enabled = _targetFoeCenter.Enabled = _targetFoeRight.Enabled = false;
                        _targetAllyLeft.LineRightVisible = _targetAllyCenter.LineRightVisible = _targetFoeRight.LineRightVisible = _targetFoeCenter.LineRightVisible = _targetFoeCenter.LineDownVisible = _targetFoeLeft.LineDownVisible = _targetFoeRight.LineDownVisible = false;
                        break;
                    }
                    case PBEMoveTarget.SingleFoeSurrounding:
                    {
                        if (pkmn.FieldPosition == PBEFieldPosition.Left)
                        {
                            _selection = TargetSelection.FoeRight;
                            _targetFoeCenter.Enabled = _targetFoeRight.Enabled = true;
                            _targetFoeLeft.Enabled = false;
                            _targetFoeCenter.Targets = PBETurnTarget.FoeCenter;
                            _targetFoeRight.Targets = PBETurnTarget.FoeRight;
                        }
                        else if (pkmn.FieldPosition == PBEFieldPosition.Center)
                        {
                            _selection = TargetSelection.FoeRight;
                            _targetFoeLeft.Enabled = _targetFoeCenter.Enabled = _targetFoeRight.Enabled = true;
                            _targetFoeLeft.Targets = PBETurnTarget.FoeLeft;
                            _targetFoeCenter.Targets = PBETurnTarget.FoeCenter;
                            _targetFoeRight.Targets = PBETurnTarget.FoeRight;
                        }
                        else
                        {
                            _selection = TargetSelection.FoeCenter;
                            _targetFoeLeft.Enabled = _targetFoeCenter.Enabled = true;
                            _targetFoeRight.Enabled = false;
                            _targetFoeLeft.Targets = PBETurnTarget.FoeLeft;
                            _targetFoeCenter.Targets = PBETurnTarget.FoeCenter;
                        }
                        _targetAllyLeft.Enabled = _targetAllyCenter.Enabled = _targetAllyRight.Enabled = false;
                        _targetAllyLeft.LineRightVisible = _targetAllyCenter.LineRightVisible = _targetFoeRight.LineRightVisible = _targetFoeCenter.LineRightVisible = _targetFoeCenter.LineDownVisible = _targetFoeLeft.LineDownVisible = _targetFoeRight.LineDownVisible = false;
                        break;
                    }
                    case PBEMoveTarget.SingleNotSelf:
                    {
                        _selection = TargetSelection.FoeRight;
                        if (pkmn.FieldPosition == PBEFieldPosition.Left)
                        {
                            _targetAllyLeft.Enabled = false;
                            _targetAllyCenter.Enabled = _targetAllyRight.Enabled = true;
                            _targetAllyCenter.Targets = PBETurnTarget.AllyCenter;
                            _targetAllyRight.Targets = PBETurnTarget.AllyRight;
                        }
                        else if (pkmn.FieldPosition == PBEFieldPosition.Center)
                        {
                            _targetAllyCenter.Enabled = false;
                            _targetAllyLeft.Enabled = _targetAllyRight.Enabled = true;
                            _targetAllyLeft.Targets = PBETurnTarget.AllyLeft;
                            _targetAllyRight.Targets = PBETurnTarget.AllyRight;
                        }
                        else
                        {
                            _targetAllyRight.Enabled = false;
                            _targetAllyLeft.Enabled = _targetAllyCenter.Enabled = true;
                            _targetAllyLeft.Targets = PBETurnTarget.AllyLeft;
                            _targetAllyCenter.Targets = PBETurnTarget.AllyCenter;
                        }
                        _targetFoeLeft.Enabled = _targetFoeCenter.Enabled = _targetFoeRight.Enabled = true;
                        _targetAllyLeft.LineRightVisible = _targetAllyCenter.LineRightVisible = _targetFoeRight.LineRightVisible = _targetFoeCenter.LineRightVisible = _targetFoeCenter.LineDownVisible = _targetFoeLeft.LineDownVisible = _targetFoeRight.LineDownVisible = false;
                        _targetFoeLeft.Targets = PBETurnTarget.FoeLeft;
                        _targetFoeCenter.Targets = PBETurnTarget.FoeCenter;
                        _targetFoeRight.Targets = PBETurnTarget.FoeRight;
                        break;
                    }
                    case PBEMoveTarget.SingleSurrounding:
                    {
                        if (pkmn.FieldPosition == PBEFieldPosition.Left)
                        {
                            _selection = TargetSelection.FoeRight;
                            _targetAllyCenter.Enabled = _targetFoeCenter.Enabled = _targetFoeRight.Enabled = true;
                            _targetAllyLeft.Enabled = _targetAllyRight.Enabled = _targetFoeLeft.Enabled = false;
                            _targetAllyCenter.Targets = PBETurnTarget.AllyCenter;
                            _targetFoeCenter.Targets = PBETurnTarget.FoeCenter;
                            _targetFoeRight.Targets = PBETurnTarget.FoeRight;
                        }
                        else if (pkmn.FieldPosition == PBEFieldPosition.Center)
                        {
                            _selection = TargetSelection.FoeRight;
                            _targetAllyLeft.Enabled = _targetAllyRight.Enabled = _targetFoeLeft.Enabled = _targetFoeCenter.Enabled = _targetFoeRight.Enabled = true;
                            _targetAllyCenter.Enabled = false;
                            _targetAllyLeft.Targets = PBETurnTarget.AllyLeft;
                            _targetAllyRight.Targets = PBETurnTarget.AllyRight;
                            _targetFoeLeft.Targets = PBETurnTarget.FoeLeft;
                            _targetFoeCenter.Targets = PBETurnTarget.FoeCenter;
                            _targetFoeRight.Targets = PBETurnTarget.FoeRight;
                        }
                        else
                        {
                            _selection = TargetSelection.FoeCenter;
                            _targetAllyCenter.Enabled = _targetFoeLeft.Enabled = _targetFoeCenter.Enabled = true;
                            _targetAllyLeft.Enabled = _targetAllyRight.Enabled = _targetFoeRight.Enabled = false;
                            _targetAllyCenter.Targets = PBETurnTarget.AllyCenter;
                            _targetFoeLeft.Targets = PBETurnTarget.FoeLeft;
                            _targetFoeCenter.Targets = PBETurnTarget.FoeCenter;
                        }
                        _targetAllyLeft.LineRightVisible = _targetAllyCenter.LineRightVisible = _targetFoeRight.LineRightVisible = _targetFoeCenter.LineRightVisible = _targetFoeCenter.LineDownVisible = _targetFoeLeft.LineDownVisible = _targetFoeRight.LineDownVisible = false;
                        break;
                    }
                    default: throw new ArgumentOutOfRangeException(nameof(possibleTargets));
                }
            }

            // This would still show the lines if a move had lines
            if (pkmn.TempLockedTargets != PBETurnTarget.None)
            {
                if (!pkmn.TempLockedTargets.HasFlag(PBETurnTarget.AllyLeft))
                {
                    _targetAllyLeft.Enabled = false;
                }
                if (!pkmn.TempLockedTargets.HasFlag(PBETurnTarget.AllyCenter))
                {
                    _targetAllyCenter.Enabled = false;
                }
                if (!pkmn.TempLockedTargets.HasFlag(PBETurnTarget.AllyRight))
                {
                    _targetAllyRight.Enabled = false;
                }
                if (!pkmn.TempLockedTargets.HasFlag(PBETurnTarget.FoeLeft))
                {
                    _targetFoeLeft.Enabled = false;
                }
                if (!pkmn.TempLockedTargets.HasFlag(PBETurnTarget.FoeCenter))
                {
                    _targetFoeCenter.Enabled = false;
                }
                if (!pkmn.TempLockedTargets.HasFlag(PBETurnTarget.FoeRight))
                {
                    _targetFoeRight.Enabled = false;
                }
            }
        }

        private TargetSelection GetSelection()
        {
            if (_selectionY == -1)
            {
                return TargetSelection.Back;
            }
            if (_selectionX == 0)
            {
                return _selectionY == 0 ? TargetSelection.FoeRight : TargetSelection.AllyLeft;
            }
            if (_centerTargetsVisible)
            {
                if (_selectionX == 1)
                {
                    return _selectionY == 0 ? TargetSelection.FoeCenter : TargetSelection.AllyCenter;
                }
                if (_selectionX == 2)
                {
                    return _selectionY == 0 ? TargetSelection.FoeLeft : TargetSelection.AllyRight;
                }
            }
            else
            {
                if (_selectionX == 1)
                {
                    return _selectionY == 0 ? TargetSelection.FoeLeft : TargetSelection.AllyRight;
                }
            }
            throw new Exception();
        }
        private void UpdateSelection()
        {
            _selection = GetSelection();
        }

        public void LogicTick()
        {
            void Back()
            {
                _cancelAction.Invoke();
            }

            if (InputManager.IsPressed(Key.A))
            {
                TargetInfo ti;
                switch (_selection)
                {
                    case TargetSelection.AllyLeft: ti = _targetAllyLeft; break;
                    case TargetSelection.AllyCenter: ti = _targetAllyCenter; break;
                    case TargetSelection.AllyRight: ti = _targetAllyRight; break;
                    case TargetSelection.FoeLeft: ti = _targetFoeLeft; break;
                    case TargetSelection.FoeCenter: ti = _targetFoeCenter; break;
                    case TargetSelection.FoeRight: ti = _targetFoeRight; break;
                    default: Back(); return;
                }
                if (ti.Enabled)
                {
                    _pkmn.TurnAction = new PBETurnAction(_pkmn, _fightMove, ti.Targets);
                    _selectAction.Invoke();
                }
                return;
            }
            if (InputManager.IsPressed(Key.B))
            {
                Back();
                return;
            }
            if (InputManager.IsPressed(Key.Left))
            {
                if (_selectionX > 0)
                {
                    _selectionX--;
                    UpdateSelection();
                }
                return;
            }
            if (InputManager.IsPressed(Key.Right))
            {
                int bounds = _centerTargetsVisible ? 2 : 1;
                if (_selectionX < bounds)
                {
                    _selectionX++;
                    UpdateSelection();
                }
                return;
            }
            if (InputManager.IsPressed(Key.Down))
            {
                if (_selectionY == 1)
                {
                    _selectionY = -1;
                    UpdateSelection();
                }
                else if (_selectionY != -1)
                {
                    _selectionY++;
                    UpdateSelection();
                }
                return;
            }
            if (InputManager.IsPressed(Key.Up))
            {
                if (_selectionY == -1)
                {
                    _selectionY = 1;
                    UpdateSelection();
                }
                else if (_selectionY > 0)
                {
                    _selectionY--;
                    UpdateSelection();
                }
                return;
            }
        }

        public unsafe void RenderTick(uint* bmpAddress, int bmpWidth, int bmpHeight)
        {
            uint lineC = RenderUtils.Color(156, 173, 247, 255);
            uint selectC = RenderUtils.Color(48, 180, 255, 200);

            RenderUtils.FillRectangle(bmpAddress, bmpWidth, bmpHeight, RenderUtils.Color(31, 31, 31, 151)); // Transparent background

            #region Lines
            if (_targetFoeRight.LineDownVisible)
            {
                RenderUtils.FillRectangle(bmpAddress, bmpWidth, bmpHeight, 0.14f, 0.3f, 0.1f, 0.4f, lineC);
            }
            if (_targetFoeLeft.LineDownVisible)
            {
                RenderUtils.FillRectangle(bmpAddress, bmpWidth, bmpHeight, 0.76f, 0.3f, 0.1f, 0.4f, lineC);
            }
            float w = _centerTargetsVisible ? 0.31f : 0.62f;
            if (_targetFoeRight.LineRightVisible)
            {
                RenderUtils.FillRectangle(bmpAddress, bmpWidth, bmpHeight, 0.19f, 0.25f, w, 0.1f, lineC);
            }
            if (_targetAllyLeft.LineRightVisible)
            {
                RenderUtils.FillRectangle(bmpAddress, bmpWidth, bmpHeight, 0.19f, 0.65f, w, 0.1f, lineC);
            }
            if (_centerTargetsVisible)
            {
                if (_targetFoeCenter.LineDownVisible)
                {
                    RenderUtils.FillRectangle(bmpAddress, bmpWidth, bmpHeight, 0.45f, 0.3f, 0.1f, 0.4f, lineC);
                }
                if (_targetFoeCenter.LineRightVisible)
                {
                    RenderUtils.FillRectangle(bmpAddress, bmpWidth, bmpHeight, 0.5f, 0.25f, w, 0.1f, lineC);
                }
                if (_targetAllyCenter.LineRightVisible)
                {
                    RenderUtils.FillRectangle(bmpAddress, bmpWidth, bmpHeight, 0.5f, 0.65f, w, 0.1f, lineC);
                }
            }
            #endregion

            if (_centerTargetsVisible)
            {
                _targetFoeCenter.Render(bmpAddress, bmpWidth, bmpHeight, 0.38f, 0.2f, _selection == TargetSelection.FoeCenter);
                _targetAllyCenter.Render(bmpAddress, bmpWidth, bmpHeight, 0.38f, 0.6f, _selection == TargetSelection.AllyCenter);
            }
            _targetFoeRight.Render(bmpAddress, bmpWidth, bmpHeight, 0.07f, 0.2f, _selection == TargetSelection.FoeRight);
            _targetFoeLeft.Render(bmpAddress, bmpWidth, bmpHeight, 0.69f, 0.2f, _selection == TargetSelection.FoeLeft);
            _targetAllyLeft.Render(bmpAddress, bmpWidth, bmpHeight, 0.07f, 0.6f, _selection == TargetSelection.AllyLeft);
            _targetAllyRight.Render(bmpAddress, bmpWidth, bmpHeight, 0.69f, 0.6f, _selection == TargetSelection.AllyRight);

            RenderUtils.FillRectangle(bmpAddress, bmpWidth, bmpHeight, 0.45f, 0.9f, 0.1f, 0.1f, RenderUtils.Color(50, 50, 50, 255)); // Back
            string str = "Back";
            Font.Default.MeasureString(str, out int strW, out int strH);
            Font.Default.DrawString(bmpAddress, bmpWidth, bmpHeight,
                RenderUtils.GetCoordinatesForCentering(bmpWidth, strW, 0.5f), RenderUtils.GetCoordinatesForCentering(bmpHeight, strH, 0.95f), str, Font.DefaultWhite_I);
            if (_selection == TargetSelection.Back)
            {
                RenderUtils.DrawRectangle(bmpAddress, bmpWidth, bmpHeight, 0.45f, 0.9f, 0.1f, 0.1f, selectC);
            }
        }
    }
}
