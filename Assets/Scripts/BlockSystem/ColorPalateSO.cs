using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "NewColorPalate", menuName = "ScriptableObjects/Color Palate")]
public class ColorPalateSO : ScriptableObject
{
    // [Title("Sky Gradient")] [ColorUsage(false, true)]
    // public Color skyTopColor;
    //
    // [ColorUsage(false, true)] public Color skyBottomColor;

    [Title("Fog Gradient")] [ColorUsage(true, true)]
    public Color fogTopColor;

    [ColorUsage(true, true)] public Color fogBottomColor;

    [Title("Island Materials")] [PreviewField(Alignment = ObjectFieldAlignment.Left)]
    public Material islandBorder;

    [PreviewField(Alignment = ObjectFieldAlignment.Left)]
    public Material islandMiddle;

    [Title("Block Materials")] [PreviewField(Alignment = ObjectFieldAlignment.Left)]
    public Material loopBlock;

    [PreviewField(Alignment = ObjectFieldAlignment.Left)]
    public Material pingPongBlock;

    [PreviewField(Alignment = ObjectFieldAlignment.Left)]
    public Material keyBlock;

    [PreviewField(Alignment = ObjectFieldAlignment.Left)]
    public Material wallBlock;

    [Title("Special Materials")] [PreviewField(Alignment = ObjectFieldAlignment.Left)]
    public Material frozen;
}