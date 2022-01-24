using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.Render.GUIs;
using Kermalis.PokemonGameEngine.Render.Images;
using Kermalis.PokemonGameEngine.Render.OpenGL;
using Kermalis.PokemonGameEngine.Render.Transitions;
using Kermalis.PokemonGameEngine.Render.World;
using Kermalis.PokemonGameEngine.Sound;
using Silk.NET.OpenGL;

namespace Kermalis.PokemonGameEngine.Render.Pkmn
{
    // TODO: Nickname
    internal sealed class EggHatchGUI
    {
        private static readonly Vec2I _renderSize = new(384, 216); // 16:9
        private readonly FrameBuffer2DColor _frameBuffer;

        private readonly PartyPokemon _pkmn;

        private ITransition _transition;

        private Window _stringWindow;
        private StringPrinter _stringPrinter;

        private AnimatedImage _img;
        private Vec2I _imgPos;

        /// <summary>Will create an egg hatch GUI. Pkmn is determined by <see cref="Var.SpecialVar1"/>'s party index.</summary>
        public EggHatchGUI()
        {
            Display.SetMinimumWindowSize(_renderSize);
            _frameBuffer = new FrameBuffer2DColor(_renderSize);

            _pkmn = Game.Instance.Save.PlayerParty[Game.Instance.Save.Vars[Var.SpecialVar1]];
            UpdatePkmnImage();

            _transition = FadeFromColorTransition.FromBlackStandard();
            Game.Instance.SetCallback(CB_FadeIn);
        }

        private void UpdatePkmnImage()
        {
            _img?.DeductReference();
            if (_pkmn.IsEgg)
            {
                _img = PokemonImageLoader.GetEggImage();
            }
            else
            {
                _img = PokemonImageLoader.GetPokemonImage(_pkmn.Species, _pkmn.Form, _pkmn.Gender, _pkmn.Shiny, _pkmn.PID, false);
            }
            _imgPos = Vec2I.Center(0.5f, 0.5f, _img.Size, _renderSize);
        }
        private void CreateMessage(string msg)
        {
            _stringPrinter = new StringPrinter(_stringWindow, msg, Font.Default, FontColors.DefaultDarkGray_I, new Vec2I(8, 0));
        }
        private bool ReadMessage()
        {
            _stringPrinter.Update();
            return _stringPrinter.IsDone;
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
            CreateMessage("An egg is hatching!");
            Game.Instance.SetCallback(CB_ReadEggHatchingMsg);
        }
        private void CB_ReadEggHatchingMsg()
        {
            Render();
            _stringWindow.Render();
            _frameBuffer.BlitToScreen();

            if (!ReadMessage())
            {
                return;
            }

            _stringPrinter.Dispose();
            _stringPrinter = null;
            _transition = new FadeToColorTransition(1f, Colors.FromRGB(200, 200, 200));
            Game.Instance.SetCallback(CB_FadeWhiteToHatch);
        }
        private void CB_FadeWhiteToHatch()
        {
            Render();
            _transition.Render(_frameBuffer);
            _frameBuffer.BlitToScreen();

            if (!_transition.IsDone)
            {
                return;
            }

            _transition.Dispose();
            _pkmn.HatchEgg();
            UpdatePkmnImage();
            _transition = new FadeFromColorTransition(1f, Colors.FromRGB(200, 200, 200));
            Game.Instance.SetCallback(CB_FadeToHatched);
        }
        private void CB_FadeToHatched()
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
            CreateMessage(string.Format("{0} hatched from the egg!", _pkmn.Nickname));
            Game.Instance.SetCallback(CB_ReadHatchedMsg);
        }
        private void CB_ReadHatchedMsg()
        {
            Render();
            _stringWindow.Render();
            _frameBuffer.BlitToScreen();

            if (!ReadMessage())
            {
                return;
            }

            _stringPrinter.Dispose();
            _stringPrinter = null;
            _stringWindow.Close();
            _stringWindow = null;
            _transition = FadeToColorTransition.ToBlackStandard();
            Game.Instance.SetCallback(CB_FadeOut);
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
            OverworldGUI.Instance.ReturnToFieldWithFadeIn();
        }

        private void Render()
        {
            GL gl = Display.OpenGL;
            _frameBuffer.UseAndViewport(gl);
            gl.ClearColor(Colors.FromRGB(31, 31, 31));
            gl.Clear(ClearBufferMask.ColorBufferBit);

            _img.Update();
            _img.Render(_imgPos);
        }
    }
}
