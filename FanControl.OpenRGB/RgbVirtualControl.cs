using FanControl.Plugins;

namespace FanControl.OpenRGB
{
  // L'interface IPluginControlSensor indique à FanControl que cet objet 
  // peut recevoir des ordres de vitesse (de 0 à 100%) via des courbes.
  public class RgbVirtualControl(string id, string name) : IPluginControlSensor
  {
    // L'identifiant unique du capteur en interne (ex: "RGB_LIQUID")
    public string Id { get; } = id;

    // Le nom qui sera affiché dans l'interface utilisateur de FanControl
    public string Name { get; } = name;

    // La valeur actuelle (le pourcentage poussé par FanControl)
    public float? Value { get; private set; } = 0f;

    // Méthode appelée par FanControl quand l'utilisateur désactive le contrôle
    public void Reset()
    {
      Value = 0f;
    }

    // C'est ICI que la magie opère. FanControl appelle cette méthode 
    // en permanence pour pousser le résultat de ta courbe de température.
    public void Set(float val)
    {
      Value = val;
    }

    // Requis par l'interface, mais inutile pour un "Control".
    // (Utilisé uniquement pour les capteurs de lecture de type IPluginDialogSensor)
    public void Update()
    {
    }
  }
}