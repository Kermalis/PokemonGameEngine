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
    internal sealed partial class PartyGUI
    {
        public enum Mode : byte
        {
            PkmnMenu,
            SelectDaycare,
            BattleSwitchIn,
            BattleReplace
        }

        private const int NUM_COLS = 2;
        private const int NUM_ROWS = 3;
        private const int MEMBER_SPACING_X = 5;
        private const int MEMBER_SPACING_Y = 5;
        private const int BOTTOM_HEIGHT = 54;

        /// <summary>The return value of selection modes</summary>
        public const short NO_PKMN_CHOSEN = -1;

        public static PartyGUI Instance { get; private set; }

        private static readonly Vec2I _renderSize = new(384, 216); // 16:9
        private static readonly Vec2I _memberSpace = new(_renderSize.X, _renderSize.Y - BOTTOM_HEIGHT);
        private static readonly Vec2I _memberSize = RenderUtils.DecideGridElementSize(_memberSpace, new Vec2I(NUM_COLS, NUM_ROWS), new Vec2I(MEMBER_SPACING_X, MEMBER_SPACING_Y));

        private readonly FrameBuffer _frameBuffer;
        private readonly TripleColorBackground _tripleColorBG;

        private readonly Mode _mode;
        private readonly bool _useGamePartyData;
        private readonly Party _gameParty;
        private readonly BattlePokemonParty _battleParty;
        private readonly List<PartyGUIMember> _members;
        private readonly ConnectedList<Sprite> _sprites;

        private ITransition _transition;
        private Action _onClosed;
        private int _selectionForSummary;

        private Window _textChoicesWindow;
        private TextGUIChoices _textChoices;
        private GUIString _message;

        private readonly GUIString _backStr;

        #region Open & Close GUI

        private PartyGUI(Mode mode, Action onClosed, bool allowBack)
        {
            Instance = this;

            _mode = mode;
            _onClosed = onClosed;

            Display.SetMinimumWindowSize(_renderSize);
            _frameBuffer = new FrameBuffer().AddColorTexture(_renderSize);

            _tripleColorBG = new TripleColorBackground();
            _sprites = new(Sprite.Sorter);
            _members = new List<PartyGUIMember>(PkmnConstants.PartyCapacity);

            _allowBack = allowBack;
            if (allowBack)
            {
                _backStr = new GUIString("Back", Font.Default, FontColors.DefaultWhite_I);
            }

            _transition = FadeFromColorTransition.FromBlackStandard();
            Game.Instance.SetCallback(CB_FadeInParty);
        }
        public PartyGUI(Party party, Mode mode, Action onClosed)
            : this(mode, onClosed, true)
        {
            _useGamePartyData = true;
            _gameParty = party;
            for (int i = 0; i < party.Count; i++)
            {
                var rect = Rect.FromSize(RenderUtils.DecideGridElementPos(_memberSpace, new Vec2I(NUM_COLS, NUM_ROWS), new Vec2I(MEMBER_SPACING_X, MEMBER_SPACING_Y), i), _memberSize);
                _members.Add(new PartyGUIMember(party[i], rect, _sprites, _members.Count == 0));
            }

            _tripleColorBG.SetColors(Colors.FromRGB(222, 50, 60), Colors.FromRGB(190, 40, 50), Colors.FromRGB(255, 180, 200));

            if (mode == Mode.SelectDaycare)
            {
                SetSelectionVar(NO_PKMN_CHOSEN);
            }
        }
        public PartyGUI(BattlePokemonParty party, Mode mode, Action onClosed)
            : this(mode, onClosed, mode != Mode.BattleReplace) // Disallow back for BattleReplace
        {
            _useGamePartyData = false;
            _battleParty = party;
            for (int i = 0; i < party.PBEParty.Count; i++)
            {
                var rect = Rect.FromSize(RenderUtils.DecideGridElementPos(_memberSpace, new Vec2I(NUM_COLS, NUM_ROWS), new Vec2I(MEMBER_SPACING_X, MEMBER_SPACING_Y), i), _memberSize);
                _members.Add(new PartyGUIMember(party[party.PBEParty[i]], rect, _sprites, _members.Count == 0)); // Use battle party's order
            }

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
            _backStr?.Delete();
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
                _ = new SummaryGUI(_gameParty[_selectionForSummary], SummaryGUI.Mode.JustView, OnSummaryClosed);
            }
            else
            {
                PBEBattlePokemon pbePkmn = _battleParty.PBEParty[_selectionForSummary];
                BattlePokemon bPkmn = _battleParty[pbePkmn];
                _ = new SummaryGUI(bPkmn, SummaryGUI.Mode.JustView, OnSummaryClosed);
            }
        }

        #endregion

        private void UpdateBounces(int oldCol, int oldRow)
        {
            _members[SelectionCoordsToPartyIndex(oldCol, oldRow)].SetBounce(false);
            if (_cursor == CursorPos.Party)
            {
                _members[SelectionCoordsToPartyIndex(_selectedMon.X, _selectedMon.Y)].SetBounce(true);
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
                if (_battleParty.PBEParty[i] == pkmn)
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
            PBEBattlePokemon pbePkmn = _battleParty.PBEParty[index];
            BattlePokemon bPkmn = _battleParty[pbePkmn];
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
            _textChoicesWindow = Window.CreateFromInnerSize(Vec2I.FromRelative(0.55f, 0.25f, _renderSize), _textChoices.GetSize(), Colors.White4, Window.Decoration.GrayRounded);
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
                    PBEBattlePokemon p = _battleParty.PBEParty[index];
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
                    PartyPokemon pkmn = _gameParty[index];
                    nickname = pkmn.Nickname;
                    _textChoices.AddOne("Check summary", () => Action_BringUpSummary(index));
                    AddFieldMovesToActions(pkmn, index);
                    break;
                }
                case Mode.SelectDaycare:
                {
                    PartyPokemon pkmn = _gameParty[index];
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
                    PBEBattlePokemon pbePkmn = _battleParty.PBEParty[index];
                    BattlePokemon bPkmn = _battleParty[pbePkmn];
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
                    PBEBattlePokemon pbePkmn = _battleParty.PBEParty[index];
                    BattlePokemon bPkmn = _battleParty[pbePkmn];
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
            _textChoicesWindow = Window.CreateFromInnerSize(Vec2I.FromRelative(0.55f, 0.25f, _renderSize), _textChoices.GetSize(), Colors.White4, Window.Decoration.GrayRounded);
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
                int index = SelectionCoordsToPartyIndex(_selectedMon.X, _selectedMon.Y);
                // Assume Mode.PkmnMenu
                SetMessage(string.Format("Do what with {0}?", _gameParty[index].Nickname));
                Game.Instance.SetCallback(CB_Choices);
            }

            Render();
            _frameBuffer.BlitToScreen();
        }

        private void Render()
        {
            for (Sprite s = _sprites.First; s is not null; s = s.Next)
            {
                s.Callback?.Invoke(s);
            }

            GL gl = Display.OpenGL;
            _frameBuffer.UseAndViewport(gl);
            _tripleColorBG.Render(gl); // No need to glClear since this will overwrite everything

            // Draw members
            for (int i = 0; i < _members.Count; i++)
            {
                _members[i].Render(_cursor == CursorPos.Party && SelectionCoordsToPartyIndex(_selectedMon.X, _selectedMon.Y) == i);
            }

            // Back button
            if (_allowBack)
            {
                GUIRenderer.Rect(_cursor == CursorPos.Back ? Colors.V4FromRGB(96, 48, 48) : Colors.V4FromRGB(48, 48, 48),
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
