using Sirenix.OdinInspector;
using UnityEngine;
using UnityUtils;

public class ColorPalateInjector : Singleton<ColorPalateInjector> {
    [InlineEditor] public ColorPalateSO colorPalateSO;


    [Button]
    public void InjectColorsIntoScene() {
        InjectFogGradientColors();
        InjectBlockMaterials();
        InjectIslandMaterials();

        // TODO: Add more injections
    }

    private void InjectBlockMaterials() {
        var blockGridInstance = BlockGrid.Instance;
        if (blockGridInstance == null) {
            Debug.LogWarning("No BlockGrid instance found in scene!");
            return;
        }

        blockGridInstance.frozenMAT = colorPalateSO.frozen;
        blockGridInstance.loopMAT = colorPalateSO.loopBlock;
        blockGridInstance.pingpongMAT = colorPalateSO.pingPongBlock;
        blockGridInstance.wallMAT = colorPalateSO.wallBlock;
        blockGridInstance.keyMAT = colorPalateSO.keyBlock;
        blockGridInstance.key_pingpongMAT = colorPalateSO.key_pingpongMAT;

        // Inject base colors
        blockGridInstance.frozenMAT.SetColor("_EmissionColor", colorPalateSO.frozenColor);

        blockGridInstance.loopMAT.SetColor("_BaseColor", colorPalateSO.loopBlockColor);
        blockGridInstance.pingpongMAT.SetColor("_BaseColor", colorPalateSO.pingPongBlockColor);
        blockGridInstance.wallMAT.SetColor("_BaseColor", colorPalateSO.wallBlockColor);
        blockGridInstance.keyMAT.SetColor("_BaseColor", colorPalateSO.keyBlockColor);
        blockGridInstance.key_pingpongMAT.SetColor("_BaseColor", colorPalateSO.key_pingpongBlockColor);
    }

    private void InjectIslandMaterials() {
        var islandObject = GameObject.FindWithTag("IslandMiddle");
        if (islandObject == null) {
            Debug.LogWarning("No object with IslandMiddle tag found!");
            return;
        }

        var mat = islandObject.GetComponent<MeshRenderer>().sharedMaterial;
        mat = colorPalateSO.islandMiddle;

        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", colorPalateSO.islandMiddleColor);
    }

    private void InjectFogGradientColors() {
        var fogGradientObject = GameObject.FindWithTag("FogGradient");

        if (fogGradientObject == null) {
            Debug.LogWarning("No object with FogGradient tag found!");
            return;
        }

        var meshRenderer = fogGradientObject.GetComponent<MeshRenderer>();
        if (meshRenderer != null) {
            var matFog = meshRenderer.sharedMaterial;
            if (matFog != null) {
                // LogShaderProperties(matFog);

                if (!matFog.HasProperty("_BottomColor")) {
                    // Debug.LogWarning("Material does not have BottomCol property!");
                }
                else {
                    matFog.SetColor("_BottomColor", colorPalateSO.fogBottomColor);
                    // Debug.Log($"Set BottomCol to {colorPalateSO.fogBottomColor}");
                }

                if (!matFog.HasProperty("_TopColor")) {
                    // Debug.LogWarning("Material does not have TopCol property!");
                }
                else {
                    matFog.SetColor("_TopColor", colorPalateSO.fogTopColor);
                    // Debug.Log($"Set TopCol to {colorPalateSO.fogTopColor}");
                }
            }
        }
    }

    public static void LogShaderProperties(Material material) {
        if (material == null) {
            Debug.LogWarning("Material is null.");
            return;
        }

        var shader = material.shader;
        var propertyCount = shader.GetPropertyCount();

        Debug.Log($"Shader: {shader.name} has {propertyCount} properties:");
        for (var i = 0; i < propertyCount; i++) {
            var propName = shader.GetPropertyName(i);
            var type = shader.GetPropertyType(i);
            Debug.Log($"  [{i}] {propName} ({type})");
        }
    }
}