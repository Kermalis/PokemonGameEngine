using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonBattleEngine.Data.Utils;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Input;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.Pkmn.Pokedata;
using Kermalis.PokemonGameEngine.Render.GUIs;
using Kermalis.PokemonGameEngine.Render.Images;
using Kermalis.PokemonGameEngine.Render.OpenGL;
using Kermalis.PokemonGameEngine.Render.Transitions;
using Kermalis.PokemonGameEngine.Render.World;
using Kermalis.PokemonGameEngine.Sound;
using Silk.NET.OpenGL;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.Render.Pkmn
{
    internal sealed class EvolutionGUI
    {
        private static readonly Vec2I _renderSize = new(384, 216); // 16:9
        private readonly FrameBuffer2DColor _frameBuffer;

        private readonly PartyPokemon _pkmn;
        private readonly string _oldNickname;
        private readonly EvolutionData.EvoData _evo;
        private readonly bool _canCancel;

        private ITransition _transition;

        private Queue<PBEMove> _learningMoves;
        private short _forgetMove;

        private Window _stringWindow;
        private StringPrinter _stringPrinter;
        private Window _textChoicesWindow;
        private TextGUIChoices _textChoices;

        private AnimatedImage _img;
        private Vec2I _imgPos;

        public EvolutionGUI(PartyPokemon pkmn, EvolutionData.EvoData evo)
        {
            Display.SetMinimumWindowSize(_renderSize);
            _frameBuffer = new FrameBuffer2DColor(_renderSize);

            _pkmn = pkmn;
            _evo = evo;
            _oldNickname = pkmn.Nickname;
            _canCancel = Evolution.CanCancelEvolution(evo.Method);
            UpdatePkmnImage();

            _transition = FadeFromColorTransition.FromBlackStandard();
            Game.Instance.SetCallback(CB_FadeIn);
        }

        private void UpdatePkmnImage()
        {
            _img?.DeductReference();
            _img = PokemonImageLoader.GetPokemonImage(_pkmn.Species, _pkmn.Form, _pkmn.Gender, _pkmn.Shiny, _pkmn.PID, false);
            _imgPos = Vec2I.Center(0.5f, 0.5f, _img.Size, _renderSize);
        }
        private void CreateMessage(string msg)
        {
            _stringPrinter = StringPrinter.CreateStandardMessageBox(_stringWindow, msg, Font.Default, FontColors.DefaultDarkGray_I, _renderSize);
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
            return _canCancel && InputManager.JustPressed(Key.B);
        }

        private void OnSummaryClosed()
        {
            Display.SetMinimumWindowSize(_renderSize);
            _stringWindow.IsInvisible = false;
            _forgetMove = Game.Instance.Save.Vars[Var.SpecialVar_Result];

            _transition = FadeFromColorTransition.FromBlackStandard();
            Game.Instance.SetCallback(CB_LearnMove_FadeFromSummary);
        }
        private void ShouldLearnMoveAction(bool value)
        {
            if (value)
            {
                _transition = new FadeToColorTransition(1f, Colors.Black3);
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
            _transition = FadeToColorTransition.ToBlackStandard();
            Game.Instance.SetCallback(CB_FadeOut);
        }

        private void CB_FadeIn()
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
            _stringWindow = Window.CreateStandardMessageBox(Colors.White4, _renderSize);
            CreateMessage(string.Format("{0} is evolving!", _oldNickname));
            Game.Instance.SetCallback(CB_ReadIsEvolvingMsg);
        }
        private void CB_ReadIsEvolvingMsg()
        {
            Render();
            _stringWindow.Render();
            _frameBuffer.BlitToScreen();

            if (!ReadMessage())
            {
                return;
            }

            _stringPrinter.Delete();
            _stringPrinter = null;
            _transition = new FadeToColorTransition(1f, Colors.FromRGB(200, 200, 200));
            Game.Instance.SetCallback(CB_FadeWhiteToEvolution);
        }
        private void CB_FadeWhiteToEvolution()
        {
            Render();
            _transition.Render(_frameBuffer);
            _frameBuffer.BlitToScreen();

            if (CancelRequested()) // Check if the player cancelled
            {
                _transition = null;
                CreateMessage(string.Format("{0} stopped evolving!", _oldNickname));
                Game.Instance.SetCallback(CB_ReadCancelledEvoMsg);
                return;
            }
            if (!_transition.IsDone)
            {
                return;
            }

            _transition.Dispose();
            if (_evo.Method == EvoMethod.Ninjask_LevelUp)
            {
                Evolution.TryCreateShedinja(_pkmn);
            }
            _pkmn.Evolve(_evo);
            UpdatePkmnImage();
            _transition = new FadeFromColorTransition(1f, Colors.FromRGB(200, 200, 200));
            Game.Instance.SetCallback(CB_FadeToEvolution);
        }
        private void CB_FadeToEvolution()
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
            SoundControl.PlayCry(_pkmn.Species, _pkmn.Form);
            CreateMessage(string.Format("{0} evolved into {1}!", _oldNickname, PBEDataProvider.Instance.GetSpeciesName(_pkmn.Species).English));
            Game.Instance.SetCallback(CB_ReadEvolvedMsg);
        }
        private void CB_ReadEvolvedMsg()
        {
            Render();
            _stringWindow.Render();
            _frameBuffer.BlitToScreen();

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
            Render();
            _stringWindow.Render();
            _frameBuffer.BlitToScreen();

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
            Render();
            _transition.Render(_frameBuffer);
            _frameBuffer.BlitToScreen();

            if (!_transition.IsDone)
            {
                return;
            }

            _transition.Dispose();
            _frameBuffer.Delete();
            _img.DeductReference();
            OverworldGUI.Instance.ReturnToFieldWithFadeInAfterEvolutionCheck();
        }

        private void CB_LearnMove_ReadWantsToLearnMsg()
        {
            Render();
            _stringWindow.Render();
            _frameBuffer.BlitToScreen();

            if (!ReadMessageEnded())
            {
                return;
            }

            TextGUIChoices.CreateStandardYesNoChoices(ShouldLearnMoveAction, _renderSize, out _textChoices, out _textChoicesWindow);
            Game.Instance.SetCallback(CB_LearnMove_HandleMultichoice);
        }
        private void CB_LearnMove_HandleMultichoice()
        {
            Render();
            _stringWindow.Render();
            _textChoicesWindow.Render();
            _frameBuffer.BlitToScreen();

            HandleMultichoice();
        }
        private void CB_LearnMove_FadeToSummary()
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
            Render();
            _transition.Render(_frameBuffer);
            _frameBuffer.BlitToScreen();

            if (!_transition.IsDone)
            {
                return;
            }

            _transition.Dispose();
            _transition = null;
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
            Render();
            _stringWindow.Render();
            _frameBuffer.BlitToScreen();

            if (!ReadMessageEnded())
            {
                return;
            }

            TextGUIChoices.CreateStandardYesNoChoices(ShouldGiveUpMoveAction, _renderSize, out _textChoices, out _textChoicesWindow);
            Game.Instance.SetCallback(CB_LearnMove_HandleMultichoice);
        }
        private void CB_LearnMove_ReadMessageThenCheckForMoreLearnableMoves()
        {
            Render();
            _stringWindow.Render();
            _frameBuffer.BlitToScreen();

            if (!ReadMessage())
            {
                return;
            }

            _stringPrinter.Delete();
            _stringPrinter = null;
            CheckForMoreLearnableMoves();
        }

        private void Render()
        {
            _frameBuffer.Use();
            GL gl = Display.OpenGL;
            gl.ClearColor(Colors.FromRGB(31, 31, 31));
            gl.Clear(ClearBufferMask.ColorBufferBit);

            _img.Update();
            _img.Render(_imgPos);
        }
    }
}
