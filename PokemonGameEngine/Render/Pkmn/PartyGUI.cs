using Kermalis.PokemonBattleEngine.Battle;
using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Input;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.Render.Battle;
using Kermalis.PokemonGameEngine.Render.GUIs;
using Kermalis.PokemonGameEngine.Render.OpenGL;
using Kermalis.PokemonGameEngine.Render.Transitions;
using Kermalis.PokemonGameEngine.Render.World;
using Kermalis.PokemonGameEngine.World.Objs;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.Render.Pkmn
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

            public GamePartyData(Party party, List<PartyGUIMember> members, ConnectedList<Sprite> sprites)
            {
                Party = party;
                foreach (PartyPokemon pkmn in party)
                {
                    members.Add(new PartyGUIMember(pkmn, sprites, members.Count == 0));
                }
            }
        }
        private sealed class BattlePartyData
        {
            public readonly BattlePokemonParty Party;

            public BattlePartyData(BattlePokemonParty party, List<PartyGUIMember> members, ConnectedList<Sprite> sprites)
            {
                Party = party;
                foreach (PBEBattlePokemon pbePkmn in party.PBEParty)
                {
                    BattlePokemon bPkmn = party[pbePkmn]; // Use battle party's order
                    members.Add(new PartyGUIMember(bPkmn, sprites, members.Count == 0));
                }
            }
        }

        public static PartyGUI Instance { get; private set; }

        private static readonly Vec2I _renderSize = new(384, 216); // 16:9
        private readonly FrameBuffer2DColor _frameBuffer;
        private readonly TripleColorBackground _tripleColorBG;

        /// <summary>The return value of selection modes, also the y index of the cancel/back button</summary>
        public const short NO_PKMN_CHOSEN = -1;

        private readonly Mode _mode;
        private readonly bool _allowBack;
        private readonly bool _useGamePartyData;
        private readonly GamePartyData _gameParty;
        private readonly BattlePartyData _battleParty;
        private readonly List<PartyGUIMember> _members;
        private readonly ConnectedList<Sprite> _sprites;

        private ITransition _transition;
        private Action _onClosed;
        private int _selectionForSummary;

        private Window _textChoicesWindow;
        private TextGUIChoices _textChoices;
        private GUIString _message;

        private int _selectionX;
        private int _selectionY;

        private readonly GUIString _backStr;

        #region Open & Close GUI

        private PartyGUI(Mode mode, Action onClosed)
        {
            Instance = this;

            _mode = mode;
            _onClosed = onClosed;

            Display.SetMinimumWindowSize(_renderSize);
            _frameBuffer = new FrameBuffer2DColor(_renderSize);

            _tripleColorBG = new TripleColorBackground();
            _sprites = new(Sprite.Sorter);
            _members = new List<PartyGUIMember>(PkmnConstants.PartyCapacity);
            _backStr = new GUIString("Back", Font.Default, FontColors.DefaultWhite_I);

            _transition = FadeFromColorTransition.FromBlackStandard();
            Game.Instance.SetCallback(CB_FadeInParty);
        }
        public PartyGUI(Party party, Mode mode, Action onClosed)
            : this(mode, onClosed)
        {
            _allowBack = true;
            _useGamePartyData = true;
            _gameParty = new GamePartyData(party, _members, _sprites);

            _tripleColorBG.SetColors(Colors.FromRGB(222, 50, 60), Colors.FromRGB(190, 40, 50), Colors.FromRGB(255, 180, 200));

            if (mode == Mode.SelectDaycare)
            {
                SetSelectionVar(NO_PKMN_CHOSEN);
            }
        }
        public PartyGUI(BattlePokemonParty party, Mode mode, Action onClosed)
            : this(mode, onClosed)
        {
            _allowBack = mode != Mode.BattleReplace; // Disallow back for BattleReplace
            _useGamePartyData = false;
            _battleParty = new BattlePartyData(party, _members, _sprites);

            _tripleColorBG.SetColors(Colors.FromRGB(85, 0, 115), Colors.FromRGB(145, 0, 195), Colors.FromRGB(100, 65, 255));

            if (mode == Mode.BattleSwitchIn)
            {
                SetSelectionVar(NO_PKMN_CHOSEN);
            }
        }

        private void ClosePartyMenu()
        {
            _transition = FadeToColorTransition.ToBlackStandard();
            Game.Instance.SetCallback(CB_FadeOutParty);
        }
        private void OnSummaryClosed()
        {
            Display.SetMinimumWindowSize(_renderSize);
            _textChoicesWindow.IsInvisible = false;

            _transition = FadeFromColorTransition.FromBlackStandard();
            Game.Instance.SetCallback(CB_FadeInThenGoToChoicesCB);
        }

        private void CB_FadeInParty()
        {
            Render();
            _transition.Render(_frameBuffer);
            _frameBuffer.BlitToScreen();

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
            _transition.Render(_frameBuffer);
            _frameBuffer.BlitToScreen();

            if (!_transition.IsDone)
            {
                return;
            }

            _transition.Dispose();
            _frameBuffer.Delete();
            _tripleColorBG.Delete();
            foreach (PartyGUIMember m in _members)
            {
                m.Delete();
            }
            _backStr.Delete();
            DeleteMessage();
            // Choices should be closed so no need to dispose them
            Instance = null;
            _onClosed();
            _onClosed = null;
        }
        private void CB_FadeInThenGoToChoicesCB()
        {
            Render();
            _transition.Render(_frameBuffer);
            _frameBuffer.BlitToScreen();

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
            _transition.Render(_frameBuffer);
            _frameBuffer.BlitToScreen();

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

            _textChoices = new TextGUIChoices(0f, 0f, backCommand: BackCommand,
                font: Font.Default, textColors: FontColors.DefaultDarkGray_I, selectedColors: FontColors.DefaultYellow_O);
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
            Vec2I s = _textChoices.GetSize();
            _textChoicesWindow = new Window(Vec2I.FromRelative(0.6f, 0.3f, _renderSize), s, Colors.White4);
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
            _textChoices = new TextGUIChoices(0f, 0f, backCommand: CloseChoicesThenGoToHandleInputs,
                font: Font.Default, textColors: FontColors.DefaultDarkGray_I, selectedColors: FontColors.DefaultYellow_O);
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
            Vec2I s = _textChoices.GetSize();
            _textChoicesWindow = new Window(Vec2I.FromRelative(0.6f, 0.3f, _renderSize), s, Colors.White4);
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
            _frameBuffer.BlitToScreen();
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
            _frameBuffer.BlitToScreen();
        }
        private void CB_HandleInputs()
        {
            HandleInputs();

            Render();
            _frameBuffer.BlitToScreen();
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
            for (Sprite s = _sprites.First; s is not null; s = s.Next)
            {
                s.Callback?.Invoke(s);
            }

            GL gl = Display.OpenGL;
            _frameBuffer.Use(gl);
            _tripleColorBG.Render(gl); // No need to glClear since this will overwrite everything

            for (int i = 0; i < _members.Count; i++)
            {
                int col = i % 2;
                int row = i / 2;
                Vec2I pos;
                pos.X = col == 0 ? _renderSize.X / 40 : (_renderSize.X / 2) + (_renderSize.X / 40);
                pos.Y = row * (_renderSize.Y / 4) + (_renderSize.Y / 20);
                _members[i].Render(pos, col == _selectionX && row == _selectionY);
            }

            // Back button
            if (_allowBack)
            {
                GUIRenderer.Rect(_selectionY == NO_PKMN_CHOSEN ? Colors.V4FromRGB(96, 48, 48) : Colors.V4FromRGB(48, 48, 48),
                    Rect.FromSize(Vec2I.FromRelative(0.5f, 0.8f, _renderSize), Vec2I.FromRelative(0.5f, 0.2f, _renderSize)));
                _backStr.Render(Vec2I.FromRelative(0.5f, 0.8f, _renderSize));
            }

            if (_message is not null)
            {
                GUIRenderer.Rect(Colors.V4FromRGB(200, 200, 200),
                    Rect.FromSize(Vec2I.FromRelative(0f, 0.8f, _renderSize), Vec2I.FromRelative(0.5f, 0.2f, _renderSize)));
                _message.Render(Vec2I.FromRelative(0f, 0.8f, _renderSize));
            }

            Window.RenderAll();
        }
    }
}
