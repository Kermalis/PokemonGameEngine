using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.GUI.Transition;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.Render.Fonts;
using Kermalis.PokemonGameEngine.Render.Images;
using Kermalis.PokemonGameEngine.Render.OpenGL;
using Kermalis.PokemonGameEngine.Sound;
using Silk.NET.OpenGL;

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
        private Pos2D _imgPos;

        public EggHatchGUI()
        {
            _pkmn = Engine.Instance.Save.PlayerParty[Engine.Instance.Save.Vars[Var.SpecialVar1]];
            LoadPkmnImage();
            _state = State.FadeIn;
            _fadeTransition = new FadeFromColorTransition(500, Colors.Black);
            Engine.Instance.SetCallback(CB_EggHatch);
            Engine.Instance.SetRCallback(RCB_EggHatch);
        }

        private void LoadPkmnImage()
        {
            _img?.DeductReference(Game.OpenGL);
            _img = PokemonImageLoader.GetPokemonImage(_pkmn.Species, _pkmn.Form, _pkmn.Gender, _pkmn.Shiny, false, false, _pkmn.PID, _pkmn.IsEgg);
            _imgPos = Pos2D.Center(0.5f, 0.5f, _img.Size);
        }
        private void CreateMessage(string msg)
        {
            _stringPrinter = StringPrinter.CreateStandardMessageBox(_stringWindow, msg, Font.Default, FontColors.DefaultDarkGray_I);
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
                        _stringWindow = Window.CreateStandardMessageBox(Colors.White);
                        CreateMessage("An egg is hatching!");
                        _state = State.AnEggIsHatchingMsg;
                    }
                    return;
                }
                case State.AnEggIsHatchingMsg:
                {
                    if (ReadMessage())
                    {
                        _stringPrinter.Delete();
                        _stringPrinter = null;
                        _fadeTransition = new FadeToColorTransition(1_000, ColorF.FromRGB(200, 200, 200));
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
                        _fadeTransition = new FadeFromColorTransition(1_000, ColorF.FromRGB(200, 200, 200));
                        _state = State.FadeToHatched;
                    }
                    return;
                }
                case State.FadeToHatched:
                {
                    if (_fadeTransition.IsDone)
                    {
                        _fadeTransition = null;
                        SoundControl.PlayCry(_pkmn.Species, _pkmn.Form);
                        CreateMessage(string.Format("{0} hatched from the egg!", _pkmn.Nickname));
                        _state = State.PkmnHatchedMsg;
                    }
                    return;
                }
                case State.PkmnHatchedMsg:
                {
                    if (ReadMessage())
                    {
                        _stringPrinter.Delete();
                        _stringPrinter = null;
                        _stringWindow.Close();
                        _stringWindow = null;
                        _fadeTransition = new FadeToColorTransition(500, Colors.Black);
                        _state = State.FadeOut;
                    }
                    return;
                }
                case State.FadeOut:
                {
                    if (_fadeTransition.IsDone)
                    {
                        _fadeTransition = null;
                        GL gl = Game.OpenGL;
                        _img.DeductReference(gl);
                        OverworldGUI.Instance.ReturnToFieldWithFadeIn();
                    }
                    return;
                }
            }
        }

        private void RCB_EggHatch(GL gl)
        {
            GLHelper.ClearColor(gl, ColorF.FromRGB(31, 31, 31));
            gl.Clear(ClearBufferMask.ColorBufferBit);

            AnimatedImage.UpdateCurrentFrameForAll();
            _img.Render(_imgPos);

            switch (_state)
            {
                case State.FadeIn:
                case State.FadeToWhite:
                case State.FadeToHatched:
                case State.FadeOut:
                {
                    _fadeTransition.Render(gl);
                    return;
                }
                case State.AnEggIsHatchingMsg:
                case State.PkmnHatchedMsg:
                {
                    _stringWindow.Render();
                    return;
                }
            }
        }
    }
}
