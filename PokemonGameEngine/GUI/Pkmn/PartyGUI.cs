using Kermalis.PokemonBattleEngine.Battle;
using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.GUI.Battle;
using Kermalis.PokemonGameEngine.GUI.Interactive;
using Kermalis.PokemonGameEngine.GUI.Transition;
using Kermalis.PokemonGameEngine.Input;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.Render.Fonts;
using Kermalis.PokemonGameEngine.Render.GUIs;
using Kermalis.PokemonGameEngine.Render.OpenGL;
using Kermalis.PokemonGameEngine.World.Objs;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.GUI.Pkmn
{
    internal sealed class PartyGUI
    {
        public enum Mode : byte
        {
            PkmnMenu,
            SelectDaycare,
            BattleSwitchIn,
            BattleReplace
        }
        private sealed class GamePartyData
        {
            public readonly Party Party;

            public GamePartyData(Party party, List<PartyGUIMember> members, SpriteList sprites)
            {
                Party = party;
                foreach (PartyPokemon pkmn in party)
                {
                    members.Add(new PartyGUIMember(pkmn, sprites));
                }
            }
        }
        private sealed class BattlePartyData
        {
            public readonly BattlePokemonParty Party;

            public BattlePartyData(BattlePokemonParty party, List<PartyGUIMember> members, SpriteList sprites)
            {
                Party = party;
                foreach (PBEBattlePokemon pbePkmn in party.PBEParty)
                {
                    BattlePokemon bPkmn = party[pbePkmn]; // Use battle party's order
                    members.Add(new PartyGUIMember(bPkmn, sprites));
                }
            }
        }

        public static PartyGUI Instance { get; private set; }

        private static readonly Size2D _renderSize = new(384, 216); // 16:9
        private FrameBuffer _frameBuffer;

        /// <summary>The return value of selection modes, also the y index of the cancel/back button</summary>
        public const short NO_PKMN_CHOSEN = -1;

        private readonly Mode _mode;
        private readonly bool _allowBack;
        private readonly bool _useGamePartyData;
        private readonly GamePartyData _gameParty;
        private readonly BattlePartyData _battleParty;
        private readonly List<PartyGUIMember> _members;
        private readonly SpriteList _sprites;

        private ITransition _transition;
        private Action _onClosed;
        private int _selectionForSummary;

        private Window _textChoicesWindow;
        private TextGUIChoices _textChoices;
        private GUIString _message;

        private int _selectionX;
        private int _selectionY;

        private GUIString _backStr;

        #region Open & Close GUI

        public PartyGUI(Party party, Mode mode, Action onClosed)
        {
            _mode = mode;
            _allowBack = true;
            _useGamePartyData = true;
            _sprites = new();
            _members = new List<PartyGUIMember>(PkmnConstants.PartyCapacity);
            _gameParty = new GamePartyData(party, _members, _sprites);

            if (mode == Mode.SelectDaycare)
            {
                SetSelectionVar(NO_PKMN_CHOSEN);
            }

            FinishConstructor(onClosed);
        }
        public PartyGUI(BattlePokemonParty party, Mode mode, Action onClosed)
        {
            _mode = mode;
            _allowBack = mode != Mode.BattleReplace; // Disallow back for BattleReplace
            _useGamePartyData = false;
            _sprites = new();
            _members = new List<PartyGUIMember>(PkmnConstants.PartyCapacity);
            _battleParty = new BattlePartyData(party, _members, _sprites);

            if (mode == Mode.BattleSwitchIn)
            {
                SetSelectionVar(NO_PKMN_CHOSEN);
            }

            FinishConstructor(onClosed);
        }
        private void FinishConstructor(Action onClosed)
        {
            Instance = this;

            _frameBuffer = FrameBuffer.CreateWithColor(_renderSize);
            _frameBuffer.Use();

            _members[0].SetBounce(true);
            _backStr = new GUIString("Back", Font.Default, FontColors.DefaultWhite_I);

            _onClosed = onClosed;

            _transition = FadeFromColorTransition.FromBlackStandard();
            Game.Instance.SetCallback(CB_FadeInParty);
        }

        private void ClosePartyMenu()
        {
            _transition = FadeToColorTransition.ToBlackStandard();
            Game.Instance.SetCallback(CB_FadeOutParty);
        }
        private void OnSummaryClosed()
        {
            _frameBuffer.Use();
            _textChoicesWindow.IsInvisible = false;
            _transition = FadeFromColorTransition.FromBlackStandard();
            Game.Instance.SetCallback(CB_FadeInThenGoToChoicesCB);
        }

        private void CB_FadeInParty()
        {
            Render();
            _transition.Render();
            _frameBuffer.RenderToScreen();

            if (!_transition.IsDone)
            {
                return;
            }

            _transition.Dispose();
            _transition = null;
            if (_mode == Mode.BattleReplace)
            {
                BattleGUI.Instance.SwitchesBuilder.SwitchesLoop(); // Init switches loop
            }
            else
            {
                Game.Instance.SetCallback(CB_HandleInputs);
            }
        }
        private void CB_FadeOutParty()
        {
            Render();
            _transition.Render();
            _frameBuffer.RenderToScreen();

            if (!_transition.IsDone)
            {
                return;
            }

            _transition.Dispose();
            foreach (PartyGUIMember m in _members)
            {
                m.Delete();
            }
            _backStr.Delete();
            DeleteMessage();
            _frameBuffer.Delete();
            // Choices should be closed so no need to dispose
            Instance = null;
            _onClosed();
            _onClosed = null;
        }
        private void CB_FadeInThenGoToChoicesCB()
        {
            Render();
            _transition.Render();
            _frameBuffer.RenderToScreen();

            if (!_transition.IsDone)
            {
                return;
            }

            _transition.Dispose();
            _transition = null;
            Game.Instance.SetCallback(CB_Choices);
        }
        private void CB_FadeOutToSummary()
        {
            Render();
            _transition.Render();
            _frameBuffer.RenderToScreen();

            if (!_transition.IsDone)
            {
                return;
            }

            _transition.Dispose();
            _transition = null;
            _textChoicesWindow.IsInvisible = true;
            if (_useGamePartyData)
            {
                _ = new SummaryGUI(_gameParty.Party[_selectionForSummary], SummaryGUI.Mode.JustView, OnSummaryClosed);
            }
            else
            {
                BattlePokemonParty party = _battleParty.Party;
                PBEBattlePokemon pbePkmn = party.PBEParty[_selectionForSummary];
                BattlePokemon bPkmn = party[pbePkmn];
                _ = new SummaryGUI(bPkmn, SummaryGUI.Mode.JustView, OnSummaryClosed);
            }
        }

        #endregion

        private int GetPartySize()
        {
            return _useGamePartyData ? _gameParty.Party.Count : _battleParty.Party.BattleParty.Length;
        }
        private int SelectionCoordsToPartyIndex(int col, int row)
        {
            if (row == NO_PKMN_CHOSEN)
            {
                return NO_PKMN_CHOSEN;
            }
            int i = row * 2 + col;
            if (i >= GetPartySize())
            {
                return NO_PKMN_CHOSEN;
            }
            return i;
        }
        private void UpdateBounces(int oldCol, int oldRow)
        {
            int i = SelectionCoordsToPartyIndex(oldCol, oldRow);
            if (i != NO_PKMN_CHOSEN)
            {
                _members[i].SetBounce(false);
            }
            i = SelectionCoordsToPartyIndex(_selectionX, _selectionY);
            if (i != NO_PKMN_CHOSEN)
            {
                _members[i].SetBounce(true);
            }
        }
        private void UpdateColors()
        {
            for (int i = 0; i < _members.Count; i++)
            {
                _members[i].UpdateColorAndRedraw();
            }
        }
        private static void SetSelectionVar(short index)
        {
            Game.Instance.Save.Vars[Var.SpecialVar_Result] = index;
        }
        private void SetMessage(string message)
        {
            _message = new GUIString(message, Font.Default, FontColors.DefaultDarkGray_I);
        }
        private void DeleteMessage()
        {
            _message?.Delete();
            _message = null;
        }

        #region Battle replacement

        private void SetBattleReplacementMessage()
        {
            SetMessage(string.Format("You must send in {0} Pokémon.", BattleGUI.Instance.SwitchesBuilder.GetNumRemaining()));
        }
        private void SelectForBattleReplacement(PBEBattlePokemon pkmn, PBEFieldPosition pos)
        {
            CloseChoices();
            SwitchesBuilder sb = BattleGUI.Instance.SwitchesBuilder;
            sb.Push(pkmn, pos);

            // Update standby color of the one we chose
            for (int i = 0; i < _members.Count; i++)
            {
                if (_battleParty.Party.PBEParty[i] == pkmn)
                {
                    _members[i].UpdateColorAndRedraw();
                    break;
                }
            }
            if (sb.GetNumRemaining() == 0)
            {
                DeleteMessage();
                ClosePartyMenu();
            }
            return;
        }
        public void NextSwitch()
        {
            SetBattleReplacementMessage();
            Game.Instance.SetCallback(CB_HandleInputs);
        }
        private void SetUpPositionQuery(int index, bool left, bool center, bool right)
        {
            BattlePokemonParty party = _battleParty.Party;
            PBEBattlePokemon pbePkmn = party.PBEParty[index];
            BattlePokemon bPkmn = party[pbePkmn];
            PartyPokemon pPkmn = bPkmn.PartyPkmn;
            SetMessage(string.Format("Send {0} where?", pPkmn.Nickname));
            CloseChoices();

            void BackCommand()
            {
                CloseChoices();
                BringUpPkmnActions(index);
            }

            _textChoices = new TextGUIChoices(0, 0, backCommand: BackCommand, font: Font.Default, textColors: FontColors.DefaultDarkGray_I, selectedColors: FontColors.DefaultYellow_O);
            if (left)
            {
                _textChoices.AddOne("Send Left", () => SelectForBattleReplacement(pbePkmn, PBEFieldPosition.Left));
            }
            if (center)
            {
                _textChoices.AddOne("Send Center", () => SelectForBattleReplacement(pbePkmn, PBEFieldPosition.Center));
            }
            if (right)
            {
                _textChoices.AddOne("Send Right", () => SelectForBattleReplacement(pbePkmn, PBEFieldPosition.Right));
            }
            _textChoices.AddOne("Cancel", BackCommand);
            Size2D s = _textChoices.GetSize();
            _textChoicesWindow = new Window(Pos2D.FromRelative(0.6f, 0.3f, _renderSize), s, Colors.White4);
            RenderChoicesOntoWindow();
            Game.Instance.SetCallback(CB_Choices);
        }

        #endregion

        #region Field Moves

        private void AddFieldMovesToActions(PartyPokemon pkmn, int index)
        {
            void Add(PBEMove move, Action command)
            {
                string str = PBEDataProvider.Instance.GetMoveName(move).English;
                _textChoices.AddOne(str, command, textColors: FontColors.DefaultBlue_I);
            }

            Moveset moves = pkmn.Moveset;
            if (moves.Contains(PBEMove.Surf))
            {
                Add(PBEMove.Surf, () => Action_FieldSurf(index));
            }
        }
        private void SetCantUseThatHere()
        {
            SetMessage("Can't use that here.");
            Game.Instance.SetCallback(CB_CantUseFieldMove);
        }

        private void Action_FieldSurf(int index)
        {
            if (!PlayerObj.Instance.CanUseSurfFromCurrentPosition())
            {
                SetCantUseThatHere();
                return;
            }
            SetSelectionVar((short)index);
            _onClosed = OverworldGUI.Instance.ReturnToFieldAndUseSurf;
            CloseChoices();
            ClosePartyMenu();
        }

        #endregion

        private void Action_SelectPartyPkmn(int index)
        {
            switch (_mode)
            {
                case Mode.SelectDaycare:
                case Mode.BattleSwitchIn:
                {
                    SetSelectionVar((short)index);
                    CloseChoices();
                    ClosePartyMenu();
                    return;
                }
                case Mode.BattleReplace:
                {
                    BattlePokemonParty party = _battleParty.Party;
                    PBEBattlePokemon p = party.PBEParty[index];
                    BattleGUI bg = BattleGUI.Instance;
                    switch (bg.Battle.BattleFormat)
                    {
                        case PBEBattleFormat.Single:
                        {
                            SelectForBattleReplacement(p, PBEFieldPosition.Center);
                            return;
                        }
                        case PBEBattleFormat.Double:
                        {
                            bool left = bg.CanUsePositionForBattleReplacement(PBEFieldPosition.Left);
                            bool right = bg.CanUsePositionForBattleReplacement(PBEFieldPosition.Right);
                            if (left && !right)
                            {
                                SelectForBattleReplacement(p, PBEFieldPosition.Left);
                                return;
                            }
                            if (!left && right)
                            {
                                SelectForBattleReplacement(p, PBEFieldPosition.Right);
                                return;
                            }
                            SetUpPositionQuery(index, true, false, true);
                            return;
                        }
                        case PBEBattleFormat.Triple:
                        case PBEBattleFormat.Rotation:
                        {
                            bool left = bg.CanUsePositionForBattleReplacement(PBEFieldPosition.Left);
                            bool center = bg.CanUsePositionForBattleReplacement(PBEFieldPosition.Center);
                            bool right = bg.CanUsePositionForBattleReplacement(PBEFieldPosition.Right);
                            if (left && !center && !right)
                            {
                                SelectForBattleReplacement(p, PBEFieldPosition.Left);
                                return;
                            }
                            if (!left && center && !right)
                            {
                                SelectForBattleReplacement(p, PBEFieldPosition.Center);
                                return;
                            }
                            if (!left && !center && right)
                            {
                                SelectForBattleReplacement(p, PBEFieldPosition.Right);
                                return;
                            }
                            SetUpPositionQuery(index, left, center, right);
                            return;
                        }
                        default: throw new Exception();
                    }
                }
                default: throw new Exception();
            }
        }
        private void Action_BringUpSummary(int index)
        {
            _selectionForSummary = index;
            _transition = FadeToColorTransition.ToBlackStandard();
            Game.Instance.SetCallback(CB_FadeOutToSummary);
        }

        private void BringUpPkmnActions(int index)
        {
            string nickname;
            _textChoices = new TextGUIChoices(0, 0, backCommand: CloseChoicesThenGoToHandleInputs, font: Font.Default, textColors: FontColors.DefaultDarkGray_I, selectedColors: FontColors.DefaultYellow_O);
            switch (_mode)
            {
                case Mode.PkmnMenu:
                {
                    PartyPokemon pkmn = _gameParty.Party[index];
                    nickname = pkmn.Nickname;
                    _textChoices.AddOne("Check summary", () => Action_BringUpSummary(index));
                    AddFieldMovesToActions(pkmn, index);
                    break;
                }
                case Mode.SelectDaycare:
                {
                    PartyPokemon pkmn = _gameParty.Party[index];
                    nickname = pkmn.Nickname;
                    if (!pkmn.IsEgg)
                    {
                        _textChoices.AddOne("Select", () => Action_SelectPartyPkmn(index));
                    }
                    _textChoices.AddOne("Check summary", () => Action_BringUpSummary(index));
                    break;
                }
                case Mode.BattleSwitchIn:
                {
                    BattlePokemonParty party = _battleParty.Party;
                    PBEBattlePokemon pbePkmn = party.PBEParty[index];
                    BattlePokemon bPkmn = party[pbePkmn];
                    PartyPokemon pPkmn = bPkmn.PartyPkmn;
                    nickname = pPkmn.Nickname;
                    // Cannot switch in if active already or fainted, or in the switch stand by
                    if (!pPkmn.IsEgg && pbePkmn.FieldPosition == PBEFieldPosition.None && pbePkmn.HP > 0 && !BattleGUI.Instance.ActionsBuilder.IsStandBy(pbePkmn))
                    {
                        _textChoices.AddOne("Switch In", () => Action_SelectPartyPkmn(index));
                    }
                    _textChoices.AddOne("Check summary", () => Action_BringUpSummary(index));
                    break;
                }
                case Mode.BattleReplace:
                {
                    BattlePokemonParty party = _battleParty.Party;
                    PBEBattlePokemon pbePkmn = party.PBEParty[index];
                    BattlePokemon bPkmn = party[pbePkmn];
                    PartyPokemon pPkmn = bPkmn.PartyPkmn;
                    nickname = pPkmn.Nickname;
                    // Cannot switch in if active already or fainted, or in the switch stand by
                    if (!pPkmn.IsEgg && pbePkmn.FieldPosition == PBEFieldPosition.None && pbePkmn.HP > 0 && !BattleGUI.Instance.SwitchesBuilder.IsStandBy(pbePkmn))
                    {
                        _textChoices.AddOne("Switch In", () => Action_SelectPartyPkmn(index));
                    }
                    _textChoices.AddOne("Check summary", () => Action_BringUpSummary(index));
                    break;
                }
                default: throw new Exception();
            }

            _textChoices.AddOne("Cancel", CloseChoicesThenGoToHandleInputs);
            Size2D s = _textChoices.GetSize();
            _textChoicesWindow = new Window(Pos2D.FromRelative(0.6f, 0.3f, _renderSize), s, Colors.White4);
            RenderChoicesOntoWindow();
            SetMessage(string.Format("Do what with {0}?", nickname));
            Game.Instance.SetCallback(CB_Choices);
        }
        private void CloseChoices()
        {
            _textChoicesWindow.Close();
            _textChoicesWindow = null;
            _textChoices.Dispose();
            _textChoices = null;
        }
        private void CloseChoicesThenGoToHandleInputs()
        {
            CloseChoices();
            if (_mode == Mode.BattleReplace)
            {
                SetBattleReplacementMessage();
            }
            else
            {
                DeleteMessage();
            }
            Game.Instance.SetCallback(CB_HandleInputs);
        }
        private void RenderChoicesOntoWindow()
        {
            _textChoices.RenderChoicesOntoWindow(_textChoicesWindow);
        }

        private void CB_Choices()
        {
            int s = _textChoices.Selected;
            _textChoices.HandleInputs();
            // Check if the window was just closed
            if (_textChoicesWindow is not null && s != _textChoices.Selected)
            {
                RenderChoicesOntoWindow(); // Update selection if it has changed
            }

            Render();
            _frameBuffer.RenderToScreen();
        }
        private void CB_CantUseFieldMove()
        {
            // Wait for input to advance the message
            if (InputManager.JustPressed(Key.A) || InputManager.JustPressed(Key.B))
            {
                int index = SelectionCoordsToPartyIndex(_selectionX, _selectionY);
                // Assume Mode.PkmnMenu
                SetMessage(string.Format("Do what with {0}?", _gameParty.Party[index].Nickname));
                Game.Instance.SetCallback(CB_Choices);
            }

            Render();
            _frameBuffer.RenderToScreen();
        }
        private void CB_HandleInputs()
        {
            HandleInputs();

            Render();
            _frameBuffer.RenderToScreen();
        }

        private void HandleInputs()
        {
            // Select a pkmn
            if (InputManager.JustPressed(Key.A))
            {
                if (_selectionY == NO_PKMN_CHOSEN)
                {
                    if (_allowBack)
                    {
                        ClosePartyMenu();
                    }
                }
                else
                {
                    int i = SelectionCoordsToPartyIndex(_selectionX, _selectionY);
                    BringUpPkmnActions(i);
                }
                return;
            }
            // Close menu or go back a pkmm
            if (InputManager.JustPressed(Key.B))
            {
                if (_mode == Mode.BattleReplace)
                {
                    SwitchesBuilder sb = BattleGUI.Instance.SwitchesBuilder;
                    if (sb.CanPop())
                    {
                        sb.Pop();
                        UpdateColors();
                    }
                }
                else if (_allowBack)
                {
                    ClosePartyMenu();
                }
                return;
            }
            if (InputManager.JustPressed(Key.Left))
            {
                if (_selectionX == 1)
                {
                    _selectionX = 0;
                    UpdateBounces(1, _selectionY);
                }
                return;
            }
            if (InputManager.JustPressed(Key.Right))
            {
                if (_selectionX == 0 && SelectionCoordsToPartyIndex(1, _selectionY) != NO_PKMN_CHOSEN)
                {
                    _selectionX = 1;
                    UpdateBounces(0, _selectionY);
                }
                return;
            }
            if (InputManager.JustPressed(Key.Down))
            {
                int oldY = _selectionY;
                if (oldY != NO_PKMN_CHOSEN)
                {
                    if (SelectionCoordsToPartyIndex(_selectionX, oldY + 1) == NO_PKMN_CHOSEN)
                    {
                        if (!_allowBack)
                        {
                            return;
                        }
                        _selectionY = NO_PKMN_CHOSEN;
                    }
                    else
                    {
                        _selectionY++;
                    }
                    UpdateBounces(_selectionX, oldY);
                }
                return;
            }
            if (InputManager.JustPressed(Key.Up))
            {
                int oldY = _selectionY;
                if (oldY == NO_PKMN_CHOSEN)
                {
                    _selectionY = (GetPartySize() - 1) / 2;
                    UpdateBounces(_selectionX, oldY);
                }
                else if (oldY > 0)
                {
                    _selectionY--;
                    UpdateBounces(_selectionX, oldY);
                }
                return;
            }
        }

        private void Render()
        {
            _sprites.DoCallbacks();

            GL gl = Display.OpenGL;
            // Background
            //Renderer.ThreeColorBackground(dst, dstW, dstH, Renderer.Color(222, 50, 60, 255), Renderer.Color(190, 40, 50, 255), Renderer.Color(255, 180, 200, 255));
            gl.ClearColor(Colors.FromRGB(31, 31, 31));
            gl.Clear(ClearBufferMask.ColorBufferBit);

            Size2D dstSize = FrameBuffer.Current.Size;
            for (uint i = 0; i < _members.Count; i++)
            {
                uint col = i % 2;
                uint row = i / 2;
                uint x = col == 0 ? dstSize.Width / 40 : (dstSize.Width / 2) + (dstSize.Width / 40);
                uint y = row * (dstSize.Height / 4) + (dstSize.Height / 20);
                _members[(int)i].Render(new Pos2D((int)x, (int)y), col == _selectionX && row == _selectionY);
            }

            // Back button
            if (_allowBack)
            {
                GUIRenderer.Instance.FillRectangle(_selectionY == NO_PKMN_CHOSEN ? Colors.V4FromRGB(96, 48, 48) : Colors.V4FromRGB(48, 48, 48), new Rect2D(Pos2D.FromRelative(0.5f, 0.8f, _renderSize), Size2D.FromRelative(0.5f, 0.2f, _renderSize)));
                _backStr.Render(Pos2D.FromRelative(0.5f, 0.8f, _renderSize));
            }

            if (_message is not null)
            {
                GUIRenderer.Instance.FillRectangle(Colors.V4FromRGB(200, 200, 200), new Rect2D(Pos2D.FromRelative(0f, 0.8f, _renderSize), Size2D.FromRelative(0.5f, 0.2f, _renderSize)));
                _message.Render(Pos2D.FromRelative(0f, 0.8f, _renderSize));
            }

            Window.RenderAll();
        }
    }
}
