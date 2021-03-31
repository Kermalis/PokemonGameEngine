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
            public bool Enabled;
            public SpritedBattlePokemon Pokemon;
            public bool LineRightVisible;
            public bool LineDownVisible;

            public PBETurnTarget Targets;
        }

        private readonly TargetInfo _targetAllyLeft = new TargetInfo();
        private readonly TargetInfo _targetAllyCenter;
        private readonly TargetInfo _targetAllyRight = new TargetInfo();
        private readonly TargetInfo _targetFoeLeft = new TargetInfo();
        private readonly TargetInfo _targetFoeCenter;
        private readonly TargetInfo _targetFoeRight = new TargetInfo();

        private readonly bool _centerTargetsVisible;
        private readonly PBEBattlePokemon _pkmn;
        private readonly PBEMove _fightMove;
        private readonly Action _selectAction;
        private readonly Action _cancelAction;

        private readonly TargetSelection _selection;

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
                _targetAllyCenter = new TargetInfo { Pokemon = GetSprited(true, PBEFieldPosition.Center) };
                _targetFoeCenter = new TargetInfo { Pokemon = GetSprited(false, PBEFieldPosition.Center) };
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

        public void LogicTick()
        {
            bool down = InputManager.IsPressed(Key.Down);
            bool up = InputManager.IsPressed(Key.Up);
            bool left = InputManager.IsPressed(Key.Left);
            bool right = InputManager.IsPressed(Key.Right);
            bool a = InputManager.IsPressed(Key.A);
            bool b = InputManager.IsPressed(Key.B);
            if (!down && !up && !a && !b && !left && !right)
            {
                return;
            }

            void Back()
            {
                _cancelAction.Invoke();
            }

            if (a)
            {
                PBETurnTarget targets;
                switch (_selection)
                {
                    case TargetSelection.AllyLeft: targets = _targetAllyLeft.Targets; break;
                    case TargetSelection.AllyCenter: targets = _targetAllyCenter.Targets; break;
                    case TargetSelection.AllyRight: targets = _targetAllyRight.Targets; break;
                    case TargetSelection.FoeLeft: targets = _targetFoeLeft.Targets; break;
                    case TargetSelection.FoeCenter: targets = _targetFoeCenter.Targets; break;
                    case TargetSelection.FoeRight: targets = _targetFoeRight.Targets; break;
                    default: Back(); return;
                }
                _pkmn.TurnAction = new PBETurnAction(_pkmn, _fightMove, targets);
                _selectAction.Invoke();
                return;
            }
            if (b)
            {
                Back();
                return;
            }
        }

        public unsafe void RenderTick(uint* bmpAddress, int bmpWidth, int bmpHeight)
        {
            uint enabledC = RenderUtils.Color(255, 0, 0, 255);
            uint disabledC = RenderUtils.Color(0, 0, 255, 255);
            uint lineC = RenderUtils.Color(0x9C, 0xAD, 0xF7, 255);
            uint selectC = RenderUtils.Color(0, 255, 0, 96);

            RenderUtils.FillRectangle(bmpAddress, bmpWidth, bmpHeight, RenderUtils.Color(31, 31, 31, 31)); // Transparent background

            if (_centerTargetsVisible)
            {
                RenderUtils.FillRectangle(bmpAddress, bmpWidth, bmpHeight, 0.4f, 0.2f, 0.2f, 0.2f, _targetFoeCenter.Enabled ? enabledC : disabledC);
                RenderUtils.FillRectangle(bmpAddress, bmpWidth, bmpHeight, 0.4f, 0.6f, 0.2f, 0.2f, _targetAllyCenter.Enabled ? enabledC : disabledC);
            }
            RenderUtils.FillRectangle(bmpAddress, bmpWidth, bmpHeight, 0.1f, 0.2f, 0.2f, 0.2f, _targetFoeRight.Enabled ? enabledC : disabledC);
            RenderUtils.FillRectangle(bmpAddress, bmpWidth, bmpHeight, 0.7f, 0.2f, 0.2f, 0.2f, _targetFoeLeft.Enabled ? enabledC : disabledC);
            RenderUtils.FillRectangle(bmpAddress, bmpWidth, bmpHeight, 0.1f, 0.6f, 0.2f, 0.2f, _targetAllyLeft.Enabled ? enabledC : disabledC);
            RenderUtils.FillRectangle(bmpAddress, bmpWidth, bmpHeight, 0.7f, 0.6f, 0.2f, 0.2f, _targetAllyRight.Enabled ? enabledC : disabledC);
            RenderUtils.FillRectangle(bmpAddress, bmpWidth, bmpHeight, 0.45f, 0.9f, 0.1f, 0.1f, RenderUtils.Color(50, 50, 50, 255)); // Back

            #region Lines
            if (_targetFoeRight.LineDownVisible)
            {
                RenderUtils.FillRectangle(bmpAddress, bmpWidth, bmpHeight, 0.15f, 0.4f, 0.1f, 0.2f, lineC);
            }
            if (_targetFoeLeft.LineDownVisible)
            {
                RenderUtils.FillRectangle(bmpAddress, bmpWidth, bmpHeight, 0.75f, 0.4f, 0.1f, 0.2f, lineC);
            }
            if (_targetFoeRight.LineRightVisible)
            {
                RenderUtils.FillRectangle(bmpAddress, bmpWidth, bmpHeight, 0.3f, 0.3f, 0.1f, 0.1f, lineC);
            }
            if (_targetAllyLeft.LineRightVisible)
            {
                RenderUtils.FillRectangle(bmpAddress, bmpWidth, bmpHeight, 0.3f, 0.65f, 0.1f, 0.1f, lineC);
            }
            if (_centerTargetsVisible)
            {
                if (_targetFoeCenter.LineDownVisible)
                {
                    RenderUtils.FillRectangle(bmpAddress, bmpWidth, bmpHeight, 0.45f, 0.4f, 0.1f, 0.2f, lineC);
                }
                if (_targetFoeCenter.LineRightVisible)
                {
                    RenderUtils.FillRectangle(bmpAddress, bmpWidth, bmpHeight, 0.6f, 0.3f, 0.1f, 0.1f, lineC);
                }
                if (_targetAllyCenter.LineRightVisible)
                {
                    RenderUtils.FillRectangle(bmpAddress, bmpWidth, bmpHeight, 0.6f, 0.65f, 0.1f, 0.1f, lineC);
                }
            }
            #endregion

            switch (_selection)
            {
                case TargetSelection.FoeLeft: RenderUtils.FillRectangle(bmpAddress, bmpWidth, bmpHeight, 0.7f, 0.2f, 0.2f, 0.2f, selectC); break;
                case TargetSelection.FoeCenter: RenderUtils.FillRectangle(bmpAddress, bmpWidth, bmpHeight, 0.4f, 0.2f, 0.2f, 0.2f, selectC); break;
                case TargetSelection.FoeRight: RenderUtils.FillRectangle(bmpAddress, bmpWidth, bmpHeight, 0.1f, 0.2f, 0.2f, 0.2f, selectC); break;
                case TargetSelection.AllyLeft: RenderUtils.FillRectangle(bmpAddress, bmpWidth, bmpHeight, 0.1f, 0.6f, 0.2f, 0.2f, selectC); break;
                case TargetSelection.AllyCenter: RenderUtils.FillRectangle(bmpAddress, bmpWidth, bmpHeight, 0.4f, 0.6f, 0.2f, 0.2f, selectC); break;
                case TargetSelection.AllyRight: RenderUtils.FillRectangle(bmpAddress, bmpWidth, bmpHeight, 0.7f, 0.6f, 0.2f, 0.2f, selectC); break;
                default: RenderUtils.FillRectangle(bmpAddress, bmpWidth, bmpHeight, 0.45f, 0.9f, 0.1f, 0.1f, selectC); break;
            }
        }
    }
}
