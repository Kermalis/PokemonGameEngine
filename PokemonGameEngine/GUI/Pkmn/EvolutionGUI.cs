using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.GUI.Interactive;
using Kermalis.PokemonGameEngine.GUI.Transition;
using Kermalis.PokemonGameEngine.Input;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.Pkmn.Pokedata;
using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.Sound;
using Kermalis.PokemonGameEngine.UI;
using Kermalis.PokemonGameEngine.Util;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.GUI.Pkmn
{
    internal sealed class EvolutionGUI
    {
        private enum State : byte
        {
            FadeIn,
            IsEvolvingMsg,
            FadeToWhite,
            FadeToEvo,
            EvolvedIntoMsg,
            FadeOut,
            CancelledMsg,
            LearnMove_WantsToLearnMoveMsg,
            LearnMove_WantsToLearnMoveChoice,
            LearnMove_FadeToSummary,
            LearnMove_FadeFromSummary,
            LearnMove_GiveUpLearningMsg,
            LearnMove_GiveUpLearningChoice,
            LearnMove_DidNotLearnMsg,
            LearnMove_ForgotMsg
        }
        private State _state;
        private readonly PartyPokemon _pkmn;
        private readonly string _oldNickname;
        private readonly EvolutionData.EvoData _evo;
        private readonly bool _canCancel;

        private FadeColorTransition _fadeTransition;

        private Queue<PBEMove> _learningMoves;
        private int _forgetMove;

        private Window _stringWindow;
        private StringPrinter _stringPrinter;
        private Window _textChoicesWindow;
        private TextGUIChoices _textChoices;

        private AnimatedImage _img;
        private int _imgX;
        private int _imgY;

        public unsafe EvolutionGUI(PartyPokemon pkmn, EvolutionData.EvoData evo)
        {
            _pkmn = pkmn;
            _evo = evo;
            _oldNickname = pkmn.Nickname;
            _canCancel = Evolution.CanCancelEvolution(evo.Method);
            LoadPkmnImage();
            _state = State.FadeIn;
            _fadeTransition = new FadeFromColorTransition(500, 0);
            Game.Instance.SetCallback(CB_Evolution);
            Game.Instance.SetRCallback(RCB_Evolution);
        }

        private void LoadPkmnImage()
        {
            _img = PokemonImageUtils.GetPokemonImage(_pkmn.Species, _pkmn.Form, _pkmn.Gender, _pkmn.Shiny, false, false, _pkmn.PID, _pkmn.IsEgg);
            _imgX = RenderUtils.GetCoordinatesForCentering(Program.RenderWidth, _img.Width, 0.5f);
            _imgY = RenderUtils.GetCoordinatesForCentering(Program.RenderHeight, _img.Height, 0.5f);
        }
        private void CreateMessage(string msg)
        {
            _stringPrinter = new StringPrinter(_stringWindow, msg, 0.1f, 0.01f, Font.Default, Font.DefaultDarkGray_I);
        }
        private bool ReadMessage()
        {
            _stringPrinter.LogicTick();
            return _stringPrinter.IsDone;
        }
        private bool ReadMessageEnded()
        {
            _stringPrinter.LogicTick();
            return _stringPrinter.IsEnded;
        }
        private bool TryCancelEvolution()
        {
            return _canCancel && InputManager.IsPressed(Key.B);
        }

        private unsafe void OnSummaryClosed()
        {
            _stringWindow.IsInvisible = false;
            _forgetMove = Game.Instance.Save.Vars[Var.SpecialVar_Result];

            _fadeTransition = new FadeFromColorTransition(500, 0);
            Game.Instance.SetCallback(CB_Evolution);
            Game.Instance.SetRCallback(RCB_Evolution);
            _state = State.LearnMove_FadeFromSummary;
        }
        private void ShouldLearnMoveAction(bool value)
        {
            if (value)
            {
                _fadeTransition = new FadeToColorTransition(1_000, 0);
                _state = State.LearnMove_FadeToSummary;
            }
            else
            {
                _textChoicesWindow.Close();
                _textChoicesWindow = null;
                _textChoices.Dispose();
                _textChoices = null;
                _stringPrinter.Close();
                _stringPrinter = null;
                SetGiveUpLearningMove();
            }
        }
        private void ShouldGiveUpMoveAction(bool value)
        {
            _textChoicesWindow.Close();
            _textChoicesWindow = null;
            _textChoices.Dispose();
            _textChoices = null;
            _stringPrinter.Close();
            _stringPrinter = null;
            if (value)
            {
                PBEMove move = _learningMoves.Dequeue(); // Remove from queue
                string str = PBELocalizedString.GetMoveName(move).English;
                CreateMessage(string.Format("{0} did not learn {1}.", _pkmn.Nickname, str));
                _state = State.LearnMove_DidNotLearnMsg;
            }
            else
            {
                SetWantsToLearnMove();
            }
        }
        private void HandleMultichoice()
        {
            int s = _textChoices.Selected;
            _textChoices.HandleInputs();
            if (!(_textChoicesWindow is null)) // Was not just closed
            {
                if (s != _textChoices.Selected)
                {
                    _textChoices.RenderChoicesOntoWindow(_textChoicesWindow);
                }
            }
        }
        private void CheckForLearnMoves()
        {
            if (_learningMoves.Count != 0)
            {
                int index = _pkmn.Moveset.GetFirstEmptySlot();
                if (index == -1)
                {
                    SetWantsToLearnMove();
                }
                else
                {
                    Moveset.MovesetSlot slot = _pkmn.Moveset[index];
                    PBEMove move = _learningMoves.Dequeue(); // Remove from queue
                    string moveStr = PBELocalizedString.GetMoveName(move).English;
                    slot.Move = move;
                    PBEMoveData mData = PBEMoveData.Data[move];
                    slot.PP = PBEDataUtils.CalcMaxPP(mData.PPTier, 0, PkmnConstants.PBESettings);
                    slot.PPUps = 0;
                    CreateMessage(string.Format("{0} learned {1}!", _pkmn.Nickname, moveStr));
                    _state = State.LearnMove_ForgotMsg;
                }
            }
            else
            {
                SetFadeOut();
            }
        }

        private void SetWantsToLearnMove()
        {
            PBEMove move = _learningMoves.Peek();
            string str = PBELocalizedString.GetMoveName(move).English;
            CreateMessage(string.Format("{0} wants to learn {1},\nbut {0} already knows {2} moves.\fForget a move and learn {1}?", _pkmn.Nickname, str, PkmnConstants.NumMoves));
            _state = State.LearnMove_WantsToLearnMoveMsg;
        }
        private void SetGiveUpLearningMove()
        {
            PBEMove move = _learningMoves.Peek();
            string str = PBELocalizedString.GetMoveName(move).English;
            CreateMessage(string.Format("Give up on learning {0}?", str));
            _state = State.LearnMove_GiveUpLearningMsg;
        }
        private void SetFadeOut()
        {
            _stringWindow.Close();
            _stringWindow = null;
            _fadeTransition = new FadeToColorTransition(500, 0);
            _state = State.FadeOut;
        }

        private void CB_Evolution()
        {
            switch (_state)
            {
                case State.FadeIn:
                {
                    if (_fadeTransition.IsDone)
                    {
                        _fadeTransition = null;
                        _stringWindow = new Window(0, 0.79f, 1, 0.16f, RenderUtils.Color(255, 255, 255, 255));
                        CreateMessage(string.Format("{0} is evolving!", _oldNickname));
                        _state = State.IsEvolvingMsg;
                    }
                    return;
                }
                case State.IsEvolvingMsg:
                {
                    if (ReadMessage())
                    {
                        _stringPrinter.Close();
                        _stringPrinter = null;
                        _fadeTransition = new FadeToColorTransition(1_000, RenderUtils.ColorNoA(200, 200, 200));
                        _state = State.FadeToWhite;
                    }
                    return;
                }
                case State.FadeToWhite:
                {
                    if (TryCancelEvolution())
                    {
                        _fadeTransition = null;
                        CreateMessage(string.Format("{0} stopped evolving!", _oldNickname));
                        _state = State.CancelledMsg;
                        return;
                    }
                    if (_fadeTransition.IsDone)
                    {
                        _fadeTransition = null;
                        if (_evo.Method == EvoMethod.Ninjask_LevelUp)
                        {
                            Evolution.TryCreateShedinja(_pkmn);
                        }
                        _pkmn.Evolve(_evo);
                        LoadPkmnImage();
                        _fadeTransition = new FadeFromColorTransition(1_000, RenderUtils.ColorNoA(200, 200, 200));
                        _state = State.FadeToEvo;
                    }
                    return;
                }
                case State.FadeToEvo:
                {
                    if (_fadeTransition.IsDone)
                    {
                        _fadeTransition = null;
                        SoundControl.Debug_PlayCry(_pkmn.Species, _pkmn.Form);
                        CreateMessage(string.Format("{0} evolved into {1}!", _oldNickname, PBELocalizedString.GetSpeciesName(_pkmn.Species).English));
                        _state = State.EvolvedIntoMsg;
                    }
                    return;
                }
                case State.EvolvedIntoMsg:
                {
                    if (ReadMessage())
                    {
                        _stringPrinter.Close();
                        _stringPrinter = null;
                        // Check for moves to learn
                        _learningMoves = new Queue<PBEMove>(new LevelUpData(_pkmn.Species, _pkmn.Form).GetNewMoves(_pkmn.Level));
                        CheckForLearnMoves();
                    }
                    return;
                }
                case State.FadeOut:
                {
                    if (_fadeTransition.IsDone)
                    {
                        _fadeTransition = null;
                        OverworldGUI.Instance.ReturnToFieldWithFadeInAfterEvolutionCheck();
                    }
                    return;
                }
                case State.CancelledMsg:
                {
                    if (ReadMessage())
                    {
                        _stringPrinter.Close();
                        _stringPrinter = null;
                        SetFadeOut();
                    }
                    return;
                }
                // Learning moves
                case State.LearnMove_WantsToLearnMoveMsg:
                {
                    if (ReadMessageEnded())
                    {
                        TextGUIChoices.CreateStandardYesNoChoices(ShouldLearnMoveAction, out _textChoices, out _textChoicesWindow);
                        _state = State.LearnMove_WantsToLearnMoveChoice;
                    }
                    return;
                }
                case State.LearnMove_WantsToLearnMoveChoice:
                case State.LearnMove_GiveUpLearningChoice:
                {
                    HandleMultichoice();
                    return;
                }
                case State.LearnMove_FadeToSummary:
                {
                    if (_fadeTransition.IsDone)
                    {
                        _fadeTransition = null;
                        _stringWindow.IsInvisible = true;
                        _textChoicesWindow.Close();
                        _textChoicesWindow = null;
                        _textChoices.Dispose();
                        _textChoices = null;
                        _stringPrinter.Close();
                        _stringPrinter = null;
                        _ = new SummaryGUI(_pkmn, SummaryGUI.Mode.LearnMove, OnSummaryClosed, learningMove: _learningMoves.Peek());
                    }
                    return;
                }
                case State.LearnMove_FadeFromSummary:
                {
                    if (_fadeTransition.IsDone)
                    {
                        // Give up on learning
                        if (_forgetMove == -1 || _forgetMove == PkmnConstants.NumMoves)
                        {
                            SetGiveUpLearningMove();
                        }
                        else
                        {
                            Moveset.MovesetSlot slot = _pkmn.Moveset[_forgetMove];
                            PBEMove oldMove = slot.Move;
                            string oldMoveStr = PBELocalizedString.GetMoveName(oldMove).English;
                            PBEMove move = _learningMoves.Dequeue(); // Remove from queue
                            string moveStr = PBELocalizedString.GetMoveName(move).English;
                            slot.Move = move;
                            PBEMoveData mData = PBEMoveData.Data[move];
                            slot.PP = PBEDataUtils.CalcMaxPP(mData.PPTier, 0, PkmnConstants.PBESettings);
                            slot.PPUps = 0;
                            CreateMessage(string.Format("{0} forgot {1}\nand learned {2}!", _pkmn.Nickname, oldMoveStr, moveStr));
                            _state = State.LearnMove_ForgotMsg;
                        }
                    }
                    return;
                }
                case State.LearnMove_GiveUpLearningMsg:
                {
                    if (ReadMessageEnded())
                    {
                        TextGUIChoices.CreateStandardYesNoChoices(ShouldGiveUpMoveAction, out _textChoices, out _textChoicesWindow);
                        _state = State.LearnMove_GiveUpLearningChoice;
                    }
                    return;
                }
                case State.LearnMove_DidNotLearnMsg:
                case State.LearnMove_ForgotMsg:
                {
                    if (ReadMessage())
                    {
                        _stringPrinter.Close();
                        _stringPrinter = null;
                        CheckForLearnMoves();
                    }
                    return;
                }
            }
        }

        private unsafe void RCB_Evolution(uint* bmpAddress, int bmpWidth, int bmpHeight)
        {
            RenderUtils.OverwriteRectangle(bmpAddress, bmpWidth, bmpHeight, RenderUtils.Color(30, 30, 30, 255));

            AnimatedImage.UpdateCurrentFrameForAll();
            _img.DrawOn(bmpAddress, bmpWidth, bmpHeight, _imgX, _imgY);

            switch (_state)
            {
                case State.FadeIn:
                case State.FadeToWhite:
                case State.FadeToEvo:
                case State.FadeOut:
                case State.LearnMove_FadeToSummary:
                case State.LearnMove_FadeFromSummary:
                {
                    _fadeTransition.RenderTick(bmpAddress, bmpWidth, bmpHeight);
                    return;
                }
                case State.IsEvolvingMsg:
                case State.EvolvedIntoMsg:
                case State.CancelledMsg:
                case State.LearnMove_WantsToLearnMoveMsg:
                case State.LearnMove_GiveUpLearningMsg:
                case State.LearnMove_DidNotLearnMsg:
                case State.LearnMove_ForgotMsg:
                {
                    _stringWindow.Render(bmpAddress, bmpWidth, bmpHeight);
                    return;
                }
                case State.LearnMove_WantsToLearnMoveChoice:
                case State.LearnMove_GiveUpLearningChoice:
                {
                    _stringWindow.Render(bmpAddress, bmpWidth, bmpHeight);
                    _textChoicesWindow.Render(bmpAddress, bmpWidth, bmpHeight);
                    return;
                }
            }
        }
    }
}
