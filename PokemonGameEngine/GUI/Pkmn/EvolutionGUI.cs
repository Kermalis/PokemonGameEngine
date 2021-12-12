using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonBattleEngine.Data.Utils;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.GUI.Interactive;
using Kermalis.PokemonGameEngine.GUI.Transition;
using Kermalis.PokemonGameEngine.Input;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.Pkmn.Pokedata;
using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.Render.Fonts;
using Kermalis.PokemonGameEngine.Render.Images;
using Kermalis.PokemonGameEngine.Sound;
using Silk.NET.OpenGL;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.GUI.Pkmn
{
    internal sealed class EvolutionGUI
    {
        private readonly PartyPokemon _pkmn;
        private readonly string _oldNickname;
        private readonly EvolutionData.EvoData _evo;
        private readonly bool _canCancel;

        private FadeColorTransition _fadeTransition;

        private Queue<PBEMove> _learningMoves;
        private short _forgetMove;

        private Window _stringWindow;
        private StringPrinter _stringPrinter;
        private Window _textChoicesWindow;
        private TextGUIChoices _textChoices;

        private AnimatedImage _img;
        private Pos2D _imgPos;

        public EvolutionGUI(PartyPokemon pkmn, EvolutionData.EvoData evo)
        {
            _pkmn = pkmn;
            _evo = evo;
            _oldNickname = pkmn.Nickname;
            _canCancel = Evolution.CanCancelEvolution(evo.Method);
            LoadPkmnImage();

            _fadeTransition = FadeFromColorTransition.FromBlackStandard();
            Game.Instance.SetCallback(CB_FadeIn);
        }

        private void LoadPkmnImage()
        {
            _img?.DeductReference();
            _img = PokemonImageLoader.GetPokemonImage(_pkmn.Species, _pkmn.Form, _pkmn.Gender, _pkmn.Shiny, false, false, _pkmn.PID, _pkmn.IsEgg);
            _imgPos = Pos2D.Center(0.5f, 0.5f, _img.Size);
        }
        private void CreateMessage(string msg)
        {
            _stringPrinter = StringPrinter.CreateStandardMessageBox(_stringWindow, msg, Font.Default, FontColors.DefaultDarkGray_I);
        }
        private bool ReadMessage()
        {
            _stringPrinter.Update();
            return _stringPrinter.IsDone;
        }
        private bool ReadMessageEnded()
        {
            _stringPrinter.Update();
            return _stringPrinter.IsEnded;
        }
        private bool CancelRequested()
        {
            return _canCancel && InputManager.IsPressed(Key.B);
        }

        private void OnSummaryClosed()
        {
            _stringWindow.IsInvisible = false;
            _forgetMove = Game.Instance.Save.Vars[Var.SpecialVar_Result];

            _fadeTransition = FadeFromColorTransition.FromBlackStandard();
            Game.Instance.SetCallback(CB_LearnMove_FadeFromSummary);
        }
        private void ShouldLearnMoveAction(bool value)
        {
            if (value)
            {
                _fadeTransition = new FadeToColorTransition(1f, Colors.Black4);
                Game.Instance.SetCallback(CB_LearnMove_FadeToSummary);
            }
            else
            {
                _textChoicesWindow.Close();
                _textChoicesWindow = null;
                _textChoices.Dispose();
                _textChoices = null;
                _stringPrinter.Delete();
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
            _stringPrinter.Delete();
            _stringPrinter = null;
            if (value)
            {
                PBEMove move = _learningMoves.Dequeue(); // Remove from queue
                string str = PBEDataProvider.Instance.GetMoveName(move).English;
                CreateMessage(string.Format("{0} did not learn {1}.", _pkmn.Nickname, str));
                Game.Instance.SetCallback(CB_LearnMove_ReadMessageThenCheckForMoreLearnableMoves);
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
            // Check if the window was just closed
            if (_textChoicesWindow is not null && s != _textChoices.Selected) // Was not just closed
            {
                _textChoices.RenderChoicesOntoWindow(_textChoicesWindow); // Update selection if it has changed
            }
        }
        private void CheckForMoreLearnableMoves()
        {
            if (_learningMoves.Count == 0)
            {
                InitFadeOut(); // No more
                return;
            }

            int index = _pkmn.Moveset.GetFirstEmptySlot();
            if (index == -1)
            {
                SetWantsToLearnMove(); // No empty spots, ask for one to forget
            }
            else
            {
                // Auto learn in free spot
                Moveset.MovesetSlot slot = _pkmn.Moveset[index];
                PBEMove move = _learningMoves.Dequeue(); // Remove from queue
                string moveStr = PBEDataProvider.Instance.GetMoveName(move).English;
                slot.Move = move;
                IPBEMoveData mData = PBEDataProvider.Instance.GetMoveData(move);
                slot.PP = PBEDataUtils.CalcMaxPP(mData.PPTier, 0, PkmnConstants.PBESettings);
                slot.PPUps = 0;
                CreateMessage(string.Format("{0} learned {1}!", _pkmn.Nickname, moveStr));
                Game.Instance.SetCallback(CB_LearnMove_ReadMessageThenCheckForMoreLearnableMoves);
            }
        }

        private void SetWantsToLearnMove()
        {
            PBEMove move = _learningMoves.Peek();
            string str = PBEDataProvider.Instance.GetMoveName(move).English;
            CreateMessage(string.Format("{0} wants to learn {1},\nbut {0} already knows {2} moves.\fForget a move and learn {1}?", _pkmn.Nickname, str, PkmnConstants.NumMoves));
            Game.Instance.SetCallback(CB_LearnMove_ReadWantsToLearnMsg);
        }
        private void SetGiveUpLearningMove()
        {
            PBEMove move = _learningMoves.Peek();
            string str = PBEDataProvider.Instance.GetMoveName(move).English;
            CreateMessage(string.Format("Give up on learning {0}?", str));
            Game.Instance.SetCallback(CB_LearnMove_ReadGiveUpLearningMsg);
        }
        private void InitFadeOut()
        {
            _stringWindow.Close();
            _stringWindow = null;
            _fadeTransition = FadeToColorTransition.ToBlackStandard();
            Game.Instance.SetCallback(CB_FadeOut);
        }

        private void CB_FadeIn()
        {
            RenderFading();
            if (!_fadeTransition.IsDone)
            {
                return;
            }

            _fadeTransition = null;
            _stringWindow = Window.CreateStandardMessageBox(Colors.White4);
            CreateMessage(string.Format("{0} is evolving!", _oldNickname));
            Game.Instance.SetCallback(CB_ReadIsEvolvingMsg);
        }
        private void CB_ReadIsEvolvingMsg()
        {
            RenderWithWindow();
            if (!ReadMessage())
            {
                return;
            }

            _stringPrinter.Delete();
            _stringPrinter = null;
            _fadeTransition = new FadeToColorTransition(1f, Colors.V4FromRGB(200, 200, 200));
            Game.Instance.SetCallback(CB_FadeWhiteToEvolution);
        }
        private void CB_FadeWhiteToEvolution()
        {
            RenderFading();
            if (CancelRequested()) // Check if the player cancelled
            {
                _fadeTransition = null;
                CreateMessage(string.Format("{0} stopped evolving!", _oldNickname));
                Game.Instance.SetCallback(CB_ReadCancelledEvoMsg);
                return;
            }
            if (!_fadeTransition.IsDone)
            {
                return;
            }

            _fadeTransition = null;
            if (_evo.Method == EvoMethod.Ninjask_LevelUp)
            {
                Evolution.TryCreateShedinja(_pkmn);
            }
            _pkmn.Evolve(_evo);
            LoadPkmnImage();
            _fadeTransition = new FadeFromColorTransition(1f, Colors.V4FromRGB(200, 200, 200));
            Game.Instance.SetCallback(CB_FadeToEvolution);
        }
        private void CB_FadeToEvolution()
        {
            RenderFading();
            if (!_fadeTransition.IsDone)
            {
                return;
            }

            _fadeTransition = null;
            SoundControl.PlayCry(_pkmn.Species, _pkmn.Form);
            CreateMessage(string.Format("{0} evolved into {1}!", _oldNickname, PBEDataProvider.Instance.GetSpeciesName(_pkmn.Species).English));
            Game.Instance.SetCallback(CB_ReadEvolvedMsg);
        }
        private void CB_ReadEvolvedMsg()
        {
            RenderWithWindow();
            if (!ReadMessage())
            {
                return;
            }

            _stringPrinter.Delete();
            _stringPrinter = null;
            // Check for moves to learn
            _learningMoves = new Queue<PBEMove>(new LevelUpData(_pkmn.Species, _pkmn.Form).GetNewMoves(_pkmn.Level));
            CheckForMoreLearnableMoves();
        }

        private void CB_ReadCancelledEvoMsg()
        {
            RenderWithWindow();
            if (!ReadMessage())
            {
                return;
            }

            _stringPrinter.Delete();
            _stringPrinter = null;
            InitFadeOut();
        }
        private void CB_FadeOut()
        {
            RenderFading();
            if (!_fadeTransition.IsDone)
            {
                return;
            }

            _fadeTransition = null;
            _img.DeductReference();
            OverworldGUI.Instance.ReturnToFieldWithFadeInAfterEvolutionCheck();
        }

        private void CB_LearnMove_ReadWantsToLearnMsg()
        {
            RenderWithWindow();
            if (!ReadMessageEnded())
            {
                return;
            }

            TextGUIChoices.CreateStandardYesNoChoices(ShouldLearnMoveAction, out _textChoices, out _textChoicesWindow);
            Game.Instance.SetCallback(CB_LearnMove_HandleMultichoice);
        }
        private void CB_LearnMove_HandleMultichoice()
        {
            RenderWithWindowAndChoices();
            HandleMultichoice();
        }
        private void CB_LearnMove_FadeToSummary()
        {
            RenderFading();
            if (!_fadeTransition.IsDone)
            {
                return;
            }

            _fadeTransition = null;
            _stringWindow.IsInvisible = true;
            _textChoicesWindow.Close();
            _textChoicesWindow = null;
            _textChoices.Dispose();
            _textChoices = null;
            _stringPrinter.Delete();
            _stringPrinter = null;
            _ = new SummaryGUI(_pkmn, SummaryGUI.Mode.LearnMove, OnSummaryClosed, learningMove: _learningMoves.Peek());
        }
        private void CB_LearnMove_FadeFromSummary()
        {
            RenderFading();
            if (!_fadeTransition.IsDone)
            {
                return;
            }

            if (_forgetMove == SummaryGUI.NO_MOVE_CHOSEN)
            {
                SetGiveUpLearningMove(); // Give up on learning
            }
            else
            {
                // Learn move
                Moveset.MovesetSlot slot = _pkmn.Moveset[_forgetMove];
                PBEMove oldMove = slot.Move;
                string oldMoveStr = PBEDataProvider.Instance.GetMoveName(oldMove).English;
                PBEMove move = _learningMoves.Dequeue(); // Remove from queue
                string moveStr = PBEDataProvider.Instance.GetMoveName(move).English;
                slot.Move = move;
                IPBEMoveData mData = PBEDataProvider.Instance.GetMoveData(move);
                slot.PP = PBEDataUtils.CalcMaxPP(mData.PPTier, 0, PkmnConstants.PBESettings);
                slot.PPUps = 0;
                CreateMessage(string.Format("{0} forgot {1}\nand learned {2}!", _pkmn.Nickname, oldMoveStr, moveStr));
                Game.Instance.SetCallback(CB_LearnMove_ReadMessageThenCheckForMoreLearnableMoves);
            }
        }
        private void CB_LearnMove_ReadGiveUpLearningMsg()
        {
            RenderWithWindow();
            if (!ReadMessageEnded())
            {
                return;
            }

            TextGUIChoices.CreateStandardYesNoChoices(ShouldGiveUpMoveAction, out _textChoices, out _textChoicesWindow);
            Game.Instance.SetCallback(CB_LearnMove_HandleMultichoice);
        }
        private void CB_LearnMove_ReadMessageThenCheckForMoreLearnableMoves()
        {
            RenderWithWindow();
            if (!ReadMessage())
            {
                return;
            }

            _stringPrinter.Delete();
            _stringPrinter = null;
            CheckForMoreLearnableMoves();
        }

        private void RenderFading()
        {
            Render();
            _fadeTransition.Render();
        }
        private void RenderWithWindow()
        {
            Render();
            _stringWindow.Render();
        }
        private void RenderWithWindowAndChoices()
        {
            Render();
            _stringWindow.Render();
            _textChoicesWindow.Render();
        }
        private void Render()
        {
            GL gl = Display.OpenGL;
            gl.ClearColor(Colors.FromRGB(31, 31, 31));
            gl.Clear(ClearBufferMask.ColorBufferBit);

            _img.Update();
            _img.Render(_imgPos);
        }
    }
}
