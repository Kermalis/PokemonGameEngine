using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.GUI.Transition;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.Sound;
using Kermalis.PokemonGameEngine.UI;
using Kermalis.PokemonGameEngine.Util;

namespace Kermalis.PokemonGameEngine.GUI.Pkmn
{
    // TODO: Nickname
    internal sealed class EggHatchGUI
    {
        private enum State : byte
        {
            FadeIn,
            AnEggIsHatchingMsg,
            FadeToWhite,
            FadeToHatched,
            PkmnHatchedMsg,
            FadeOut
        }
        private State _state;
        private readonly PartyPokemon _pkmn;

        private FadeColorTransition _fadeTransition;

        private Window _stringWindow;
        private StringPrinter _stringPrinter;

        private AnimatedImage _img;
        private int _imgX;
        private int _imgY;

        public unsafe EggHatchGUI()
        {
            _pkmn = Game.Instance.Save.PlayerParty[Game.Instance.Save.Vars[Var.SpecialVar1]];
            LoadPkmnImage();
            _state = State.FadeIn;
            _fadeTransition = new FadeFromColorTransition(500, 0);
            Game.Instance.SetCallback(CB_EggHatch);
            Game.Instance.SetRCallback(RCB_EggHatch);
        }

        private void LoadPkmnImage()
        {
            _img = PokemonImageUtils.GetPokemonImage(_pkmn.Species, _pkmn.Form, _pkmn.Gender, _pkmn.Shiny, false, false, _pkmn.PID, _pkmn.IsEgg);
            _imgX = RenderUtils.GetCoordinatesForCentering(Program.RenderWidth, _img.Width, 0.5f);
            _imgY = RenderUtils.GetCoordinatesForCentering(Program.RenderHeight, _img.Height, 0.5f);
        }
        private void CreateMessage(string msg)
        {
            _stringPrinter = new StringPrinter(_stringWindow, msg, 0.1f, 0.01f, Font.Default, Font.DefaultDark);
        }
        private bool ReadMessage()
        {
            _stringPrinter.LogicTick();
            return _stringPrinter.IsDone;
        }

        private void CB_EggHatch()
        {
            switch (_state)
            {
                case State.FadeIn:
                {
                    if (_fadeTransition.IsDone)
                    {
                        _fadeTransition = null;
                        _stringWindow = new Window(0, 0.79f, 1, 0.16f, RenderUtils.Color(255, 255, 255, 255));
                        CreateMessage("An egg is hatching!");
                        _state = State.AnEggIsHatchingMsg;
                    }
                    return;
                }
                case State.AnEggIsHatchingMsg:
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
                    if (_fadeTransition.IsDone)
                    {
                        _fadeTransition = null;
                        _pkmn.HatchEgg();
                        LoadPkmnImage();
                        _fadeTransition = new FadeFromColorTransition(1_000, RenderUtils.ColorNoA(200, 200, 200));
                        _state = State.FadeToHatched;
                    }
                    return;
                }
                case State.FadeToHatched:
                {
                    if (_fadeTransition.IsDone)
                    {
                        _fadeTransition = null;
                        SoundControl.Debug_PlayCry(_pkmn.Species, _pkmn.Form);
                        CreateMessage(string.Format("{0} hatched from the egg!", _pkmn.Nickname));
                        _state = State.PkmnHatchedMsg;
                    }
                    return;
                }
                case State.PkmnHatchedMsg:
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
                        OverworldGUI.Instance.ReturnToFieldWithFadeIn();
                    }
                    return;
                }
            }
        }

        private unsafe void RCB_EggHatch(uint* bmpAddress, int bmpWidth, int bmpHeight)
        {
            RenderUtils.OverwriteRectangle(bmpAddress, bmpWidth, bmpHeight, RenderUtils.Color(30, 30, 30, 255));

            AnimatedImage.UpdateCurrentFrameForAll();
            _img.DrawOn(bmpAddress, bmpWidth, bmpHeight, _imgX, _imgY);

            switch (_state)
            {
                case State.FadeIn:
                case State.FadeToWhite:
                case State.FadeToHatched:
                case State.FadeOut:
                {
                    _fadeTransition.RenderTick(bmpAddress, bmpWidth, bmpHeight);
                    return;
                }
                case State.AnEggIsHatchingMsg:
                case State.PkmnHatchedMsg:
                {
                    _stringWindow.Render(bmpAddress, bmpWidth, bmpHeight);
                    return;
                }
            }
        }
    }
}
