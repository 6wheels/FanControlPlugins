using FanControl.OpenRGB.Rules;
using OpenRGB.NET;

namespace FanControl.OpenRGB.Effects
{

  // EFFECT 5: The "Crazy" Bouncing Ball & Confetti Explosion
  // 1. La classe d'origine (Bien vérifier qu'elle hérite de BaseRgbRule)
  public class BouncingExplosionRule(string deviceRegex) : BaseRgbRule(deviceRegex)
  {
    protected enum EffectState { Bouncing, Exploding, Fading }
    protected EffectState _state = EffectState.Bouncing;

    protected float _posX = 0f;
    protected float _posY = 0f;
    protected float _velX = 0.35f;
    protected float _velY = 0f;
    protected float _gravity = 0.08f;

    protected struct LedPos { public float X; public float Y; }
    protected LedPos[] _ledPositions = null!;
    protected float _maxX = 0;
    protected float _maxY = 0;

    protected float _explosionRadius = 0f;
    protected Color[] _ledState = null!;
    protected Random _rnd = new();

    // LA NOUVELLE MÉTHODE : Permet aux enfants de changer le comportement de fin
    protected virtual void OnCycleComplete()
    {
      // Par défaut, l'animation boucle à l'infini
      _state = EffectState.Bouncing;
      _posX = 0f;
      _posY = 0f;
      _velY = 0f;
      _velX = 0.35f;
      _explosionRadius = 0f;
    }

    protected override void ProcessEffect(OpenRgbClient client, Device device, int deviceIndex, float value, int frameCount)
    {
      int ledCount = device.Leds.Length;

      if (_ledPositions == null)
      {
        _ledPositions = new LedPos[ledCount];
        _ledState = Enumerable.Repeat(new Color(50, 0, 0), ledCount).ToArray();

        var matrixMap = device.Zones.FirstOrDefault()?.MatrixMap;
        if (matrixMap != null && matrixMap.Matrix != null)
        {
          _maxX = matrixMap.Width - 1;
          _maxY = matrixMap.Height - 1;

          for (uint y = 0; y < matrixMap.Height; y++)
          {
            for (uint x = 0; x < matrixMap.Width; x++)
            {
              uint idx = matrixMap.Matrix[y, x];
              if (idx != uint.MaxValue && idx < ledCount)
              {
                _ledPositions[idx] = new LedPos { X = x, Y = y };
              }
            }
          }
        }
        else
        {
          _maxX = 21; _maxY = 5;
          for (int i = 0; i < ledCount; i++) _ledPositions[i] = new LedPos { X = i % 22, Y = i / 22 };
        }
      }

      if (_state == EffectState.Bouncing)
      {
        for (int i = 0; i < ledCount; i++) _ledState[i] = new Color(50, 0, 0);

        _velY += _gravity;
        _posX += _velX;
        _posY += _velY;

        if (_posY >= _maxY)
        {
          _posY = _maxY;
          _velY *= -0.85f;
          _velX *= 0.95f;
        }

        for (int i = 0; i < ledCount; i++)
        {
          float dx = _ledPositions[i].X - _posX;
          float dy = _ledPositions[i].Y - _posY;
          float dist = (float)Math.Sqrt(dx * dx + dy * dy);

          if (dist < 0.5f) _ledState[i] = new Color(255, 255, 255);
          else if (dist < 1.2f) _ledState[i] = new Color(0, 150, 255);
        }

        if (_posX >= _maxX)
        {
          _posX = _maxX;
          _state = EffectState.Exploding;
          _explosionRadius = 0f;
        }
      }
      else if (_state == EffectState.Exploding)
      {
        _explosionRadius += 1.5f;

        for (int i = 0; i < ledCount; i++)
        {
          float dx = _ledPositions[i].X - _posX;
          float dy = _ledPositions[i].Y - _posY;
          float dist = (float)Math.Sqrt(dx * dx + dy * dy);

          if (dist < _explosionRadius && dist >= _explosionRadius - 2.0f)
          {
            int colorType = _rnd.Next(0, 5);
            _ledState[i] = colorType switch
            {
              0 => new Color(255, 0, 150),
              1 => new Color(0, 255, 255),
              2 => new Color(255, 255, 0),
              3 => new Color(0, 255, 50),
              _ => new Color(255, 100, 0),
            };
          }
        }

        if (_explosionRadius > _maxX + 5)
        {
          _state = EffectState.Fading;
        }
      }
      else if (_state == EffectState.Fading)
      {
        bool isFullyFaded = true;

        for (int i = 0; i < ledCount; i++)
        {
          byte r = FadeChannel(_ledState[i].R, 50);
          byte g = FadeChannel(_ledState[i].G, 0);
          byte b = FadeChannel(_ledState[i].B, 0);

          _ledState[i] = new Color(r, g, b);

          if (Math.Abs(r - 50) > 2 || g > 2 || b > 2) isFullyFaded = false;
        }

        if (isFullyFaded)
        {
          // L'APPEL CRUCIAL EST ICI
          OnCycleComplete();
        }
      }

      client.UpdateLeds(deviceIndex, _ledState);
    }

    private static byte FadeChannel(byte current, byte target)
    {
      int step = 2;
      if (current < target) return (byte)Math.Min(current + step, target);
      if (current > target) return (byte)Math.Max(current - step, target);
      return current;
    }
  }

  // 2. L'enfant qui désactive la boucle
  public class BootExplosionRule(string deviceRegex) : BouncingExplosionRule(deviceRegex)
  {

    // Surcharge le comportement de fin pour tuer l'animation
    protected override void OnCycleComplete()
    {
      IsFinished = true; // La propriété héritée de BaseRgbRule
    }
  }
}