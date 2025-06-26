using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "NewColorPalate", menuName = "ScriptableObjects/Color Palate")]
public class ColorPalateSO : ScriptableObject {
    // [Title("Sky Gradient")] [ColorUsage(false, true)]
    // public Color skyTopColor;
    //
    // [ColorUsage(false, true)] public Color skyBottomColor;

    [Title("Fog Gradient")]
    [ColorUsage(true, true)]
    public Color fogTopColor;

    [ColorUsage(true, true)] public Color fogBottomColor;

    // === Materials Tab ===
    [TabGroup("Materials", "Materials")]
    [Title("Island Materials")]
    [PreviewField(Alignment = ObjectFieldAlignment.Left)]
    public Material islandBorder;

    [TabGroup("Materials", "Materials")]
    [PreviewField(Alignment = ObjectFieldAlignment.Left)]
    public Material islandMiddle;

    [TabGroup("Materials", "Materials")]
    [Title("Block Materials")]
    [PreviewField(Alignment = ObjectFieldAlignment.Left)]
    public Material loopBlock;

    [TabGroup("Materials", "Materials")]
    [PreviewField(Alignment = ObjectFieldAlignment.Left)]
    public Material pingPongBlock;

    [TabGroup("Materials", "Materials")]
    [PreviewField(Alignment = ObjectFieldAlignment.Left)]
    public Material key_pingpongMAT;

    [TabGroup("Materials", "Materials")]
    [PreviewField(Alignment = ObjectFieldAlignment.Left)]
    public Material keyBlock;

    [TabGroup("Materials", "Materials")]
    [PreviewField(Alignment = ObjectFieldAlignment.Left)]
    public Material wallBlock;

    [TabGroup("Materials", "Materials")]
    [Title("Special Materials")]
    [PreviewField(Alignment = ObjectFieldAlignment.Left)]
    public Material frozen;


    // === Material Colors Tab ===
    [TabGroup("Materials", "Material Colors")]
    [Title("Island Material Colors")]
    public Color islandBorderColor;

    [TabGroup("Materials", "Material Colors")]
    public Color islandMiddleColor;

    [TabGroup("Materials", "Material Colors")]
    [Title("Block Material Colors")]
    public Color loopBlockColor;

    [TabGroup("Materials", "Material Colors")]
    public Color pingPongBlockColor;

    [TabGroup("Materials", "Material Colors")]
    public Color keyBlockColor;
    [TabGroup("Materials", "Material Colors")]
    public Color key_pingpongBlockColor;

    [TabGroup("Materials", "Material Colors")]
    public Color wallBlockColor;

    [TabGroup("Materials", "Material Colors")]
    [Title("Special Material Colors")]
    public Color frozenColor;
}