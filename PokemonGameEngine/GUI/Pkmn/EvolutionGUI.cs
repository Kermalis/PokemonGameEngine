using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.GUI.Transition;
using Kermalis.PokemonGameEngine.Input;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.Pkmn.Pokedata;
using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.Sound;
using Kermalis.PokemonGameEngine.UI;
using Kermalis.PokemonGameEngine.Util;

namespace Kermalis.PokemonGameEngine.GUI.Pkmn
{
    // TODO: Move learn
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
            CancelledMsg
        }
        private State _state;
        private readonly PartyPokemon _pkmn;
        private readonly string _oldNickname;
        private readonly EvolutionData.EvoData _evo;
        private readonly bool _canCancel;

        private FadeColorTransition _fadeTransition;

        private Window _stringWindow;
        private StringPrinter _stringPrinter;

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
        private bool TryCancelEvolution()
        {
            return _canCancel && InputManager.IsPressed(Key.B);
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
                        _stringWindow.Close();
                        _stringWindow = null;
                        _fadeTransition = new FadeToColorTransition(500, 0);
                        _state = State.FadeOut;
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
                        _stringWindow.Close();
                        _stringWindow = null;
                        _fadeTransition = new FadeToColorTransition(500, 0);
                        _state = State.FadeOut;
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
                {
                    _fadeTransition.RenderTick(bmpAddress, bmpWidth, bmpHeight);
                    return;
                }
                case State.IsEvolvingMsg:
                case State.EvolvedIntoMsg:
                case State.CancelledMsg:
                {
                    _stringWindow.Render(bmpAddress, bmpWidth, bmpHeight);
                    return;
                }
            }
        }
    }
}
