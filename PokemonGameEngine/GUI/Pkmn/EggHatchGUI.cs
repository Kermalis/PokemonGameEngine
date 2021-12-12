using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.GUI.Transition;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.Render.Fonts;
using Kermalis.PokemonGameEngine.Render.Images;
using Kermalis.PokemonGameEngine.Sound;
using Silk.NET.OpenGL;

namespace Kermalis.PokemonGameEngine.GUI.Pkmn
{
    // TODO: Nickname
    internal sealed class EggHatchGUI
    {
        private readonly PartyPokemon _pkmn;

        private FadeColorTransition _fadeTransition;

        private Window _stringWindow;
        private StringPrinter _stringPrinter;

        private AnimatedImage _img;
        private Pos2D _imgPos;

        /// <summary>Will create an egg hatch GUI. Pkmn is determined by <see cref="Var.SpecialVar1"/>'s party index.</summary>
        public EggHatchGUI()
        {
            _pkmn = Game.Instance.Save.PlayerParty[Game.Instance.Save.Vars[Var.SpecialVar1]];
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

        private void CB_FadeIn()
        {
            RenderFading();
            if (!_fadeTransition.IsDone)
            {
                return;
            }

            _fadeTransition = null;
            _stringWindow = Window.CreateStandardMessageBox(Colors.White4);
            CreateMessage("An egg is hatching!");
            Game.Instance.SetCallback(CB_ReadEggHatchingMsg);
        }
        private void CB_ReadEggHatchingMsg()
        {
            RenderWithWindow();
            if (!ReadMessage())
            {
                return;
            }

            _stringPrinter.Delete();
            _stringPrinter = null;
            _fadeTransition = new FadeToColorTransition(1f, Colors.V4FromRGB(200, 200, 200));
            Game.Instance.SetCallback(CB_FadeWhiteToHatch);
        }
        private void CB_FadeWhiteToHatch()
        {
            RenderFading();
            if (!_fadeTransition.IsDone)
            {
                return;
            }

            _fadeTransition = null;
            _pkmn.HatchEgg();
            LoadPkmnImage();
            _fadeTransition = new FadeFromColorTransition(1f, Colors.V4FromRGB(200, 200, 200));
            Game.Instance.SetCallback(CB_FadeToHatched);
        }
        private void CB_FadeToHatched()
        {
            RenderFading();
            if (!_fadeTransition.IsDone)
            {
                return;
            }

            _fadeTransition = null;
            SoundControl.PlayCry(_pkmn.Species, _pkmn.Form);
            CreateMessage(string.Format("{0} hatched from the egg!", _pkmn.Nickname));
            Game.Instance.SetCallback(CB_ReadHatchedMsg);
        }
        private void CB_ReadHatchedMsg()
        {
            RenderWithWindow();
            if (!ReadMessage())
            {
                return;
            }

            _stringPrinter.Delete();
            _stringPrinter = null;
            _stringWindow.Close();
            _stringWindow = null;
            _fadeTransition = FadeToColorTransition.ToBlackStandard();
            Game.Instance.SetCallback(CB_FadeOut);
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
            OverworldGUI.Instance.ReturnToFieldWithFadeIn();
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
