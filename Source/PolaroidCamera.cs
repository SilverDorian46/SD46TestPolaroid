using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.SD46TestPolaroid;

[CustomEntity("SD46TestEntities/PolaroidCamera"), Tracked(false)]
public class PolaroidCamera : Entity
{
    private class Photo : Entity
    {
        private class PolaroidImage(MTexture texture, RenderTarget2D renderTarget) : Image(texture)
        {
            public RenderTarget2D RenderTarget = renderTarget;

            public Vector2 RenderTargetOrigin;
            public float RenderTargetScaleMultiplier = 1;

            public override void Render()
            {
                if (Texture is not null)
                    Draw(Texture, RenderTarget, RenderPosition, Origin, RenderTargetOrigin, Color,
                        Scale, RenderTargetScaleMultiplier, Rotation, Effects);
            }

            private static void Draw(MTexture texture, RenderTarget2D renderTarget,
                Vector2 position, Vector2 origin, Vector2 renderTargetOrigin,
                Color color, Vector2 scale, float renderTargetScaleMult, float rotation, SpriteEffects flip)
            {
                float scaleFix = texture.ScaleFix;
                Monocle.Draw.SpriteBatch.Draw(texture.Texture.Texture_Safe, position, texture.ClipRect, color, rotation,
                    (origin - texture.DrawOffset) / scaleFix, scale * scaleFix,
                    flip, layerDepth: 0);
                Monocle.Draw.SpriteBatch.Draw(renderTarget, position, renderTarget.Bounds, color, rotation,
                    (renderTargetOrigin - texture.DrawOffset) / scaleFix, scale * renderTargetScaleMult * scaleFix,
                    flip, layerDepth: 0);
            }
        }

        private readonly Level level;

        private PolaroidImage? polaroidImage;

        private bool waitForKeyPress;
        private float timer;

        public Photo(Level level)
        {
            Tag = Tags.HUD;
            this.level = level;
        }

        public IEnumerator PictureRoutine(RenderTarget2D targetImage)
        {
            yield return OpenRoutine(targetImage);
            yield return WaitForInput();
            yield return EndRoutine();
        }

        private IEnumerator OpenRoutine(RenderTarget2D targetImage)
        {
            Audio.Play(SFX.game_02_theoselfie_photo_in);
            polaroidImage = new(MTN.Checkpoints["polaroid"], targetImage);
            polaroidImage.CenterOrigin();
            polaroidImage.RenderTargetOrigin = new Vector2(160 + 28, 90 + 95);
            polaroidImage.RenderTargetScaleMultiplier = 720f / 320;
            float percent = 0;
            while (percent < 1)
            {
                percent += Engine.DeltaTime;
                polaroidImage.Position = Vector2.Lerp(
                    new Vector2(992, 1080 + polaroidImage.Height / 2),
                    new Vector2(960, 540),
                    Ease.CubeOut(percent)
                );
                polaroidImage.Rotation = MathHelper.Lerp(0.5f, 0, Ease.BackOut(percent));
                yield return null;
            }
        }

        private IEnumerator WaitForInput()
        {
            waitForKeyPress = true;
            while (!Input.MenuCancel.Pressed && !Input.MenuConfirm.Pressed)
                yield return null;

            Audio.Play(SFX.ui_main_button_lowkey);
            waitForKeyPress = false;
        }

        private IEnumerator EndRoutine()
        {
            Audio.Play(SFX.game_02_theoselfie_photo_out);
            float percent = 0;
            while (percent < 1)
            {
                percent += Engine.DeltaTime;
                polaroidImage!.Position = Vector2.Lerp(
                    new Vector2(960, 540),
                    new Vector2(928, -polaroidImage.Height / 2),
                    Ease.BackIn(percent)
                );
                polaroidImage.Rotation = MathHelper.Lerp(0, -0.15f, Ease.BackIn(percent));
                yield return null;
            }

            yield return null;
            level.Remove(this);
        }

        public override void Update()
        {
            if (waitForKeyPress)
                timer += Engine.DeltaTime;
        }

        public override void Render()
        {
            if (level.FrozenOrPaused || level.RetryPlayerCorpse != null || level.SkippingCutscene)
                return;

            if (polaroidImage is null)
                return;

            if (polaroidImage.Visible)
                polaroidImage.Render();

            if (waitForKeyPress)
                GFX.Gui["textboxbutton"].DrawCentered(polaroidImage.Position + new Vector2(
                    polaroidImage.Width / 2 + 40, polaroidImage.Height / 2 + ((timer % 1 < 0.25f) ? 6 : 0)
                ));
        }
    }

    private readonly Sprite sprite;

    private VirtualRenderTarget? photoTarget;

    private Photo? photo;

    private bool takingPhoto;

    public PolaroidCamera(EntityData data, Vector2 offset) : base(data.Position + offset)
    {
        Depth = 500;

        Add(sprite = new(GFX.Game, "objects/SD46TestPolaroid/"));
        sprite.AddLoop("idle", "idle", 0.06f);
        sprite.Add("flash", "flash", 0.06f, into: "idle");
        sprite.JustifyOrigin(0.5f, 1);
        sprite.Play("idle");

        Add(new TalkComponent(new Rectangle(-16, -8, 32, 8), drawAt: Vector2.UnitY * -20, OnInteract)
        {
            PlayerMustBeFacing = false
        });

        Add(new VertexLight(Vector2.UnitY * -11, Color.White, 0.8f, 16, 32));

        Add(new BeforeRenderHook(BeforeRender));
    }

    private void OnInteract(Player player)
    {
        Add(new Coroutine(PhotoRoutine(player, SceneAs<Level>())));
    }

    private IEnumerator PhotoRoutine(Player player, Level level)
    {
        if (player.Holding is not null)
            player.Drop();
        player.StateMachine.State = Player.StDummy;
        player.Facing = player.Position.X < Position.X ? Facings.Right : Facings.Left;
        yield return 0.75f;

        sprite.Play("flash");
        Audio.Play(SFX.game_02_theoselfie_foley, Position);
        yield return 0.05f;

        takingPhoto = true;
        level.Flash(Color.White);
        yield return 0.5f;

        Scene.Add(photo = new Photo(level));
        yield return photo.PictureRoutine(photoTarget);
        photo = null;

        player.StateMachine.State = Player.StNormal;
    }

    private void BeforeRender()
    {
        if (!takingPhoto)
            return;

        photoTarget ??= VirtualContent.CreateRenderTarget("sd46-polaroid-buffer", 320, 180);

        Engine.Graphics.GraphicsDevice.SetRenderTarget(photoTarget);
        Engine.Graphics.GraphicsDevice.Clear(Color.Black);
        Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp,
            DepthStencilState.None, RasterizerState.CullNone, effect: null, Matrix.Identity);
        Draw.SpriteBatch.Draw((RenderTarget2D)GameplayBuffers.Level, Vector2.Zero, Color.White);
        Draw.SpriteBatch.End();

        takingPhoto = false;
    }
}
