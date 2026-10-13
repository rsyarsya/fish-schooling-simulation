using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class ColourGenerator : MonoBehaviour {
    public Material mat;
    public Gradient gradient;
    public float normalOffsetWeight;
    public Vector4 seaWorldParams = new Vector4 (5.08f, 1.06f, 0.4f, -0.03f);

    Texture2D texture;
    const int textureResolution = 50;

    void Init () {
        if (texture == null || texture.width != textureResolution) {
            texture = new Texture2D (textureResolution, 1, TextureFormat.RGBA32, false);
        }
    }

    void Update () {
        if (mat == null) {
            return;
        }

        Init ();
        UpdateTexture ();

        MeshGenerator m = FindObjectOfType<MeshGenerator> ();
        if (m == null) {
            return;
        }

        float boundsY = m.boundsSize * m.numChunks.y;

        mat.SetFloat ("boundsY", boundsY);
        mat.SetFloat ("normalOffsetWeight", normalOffsetWeight);
        mat.SetVector ("params", seaWorldParams);

        mat.SetTexture ("ramp", texture);
    }

    void UpdateTexture () {
        if (gradient != null) {
            Color[] colours = new Color[texture.width];
            for (int i = 0; i < textureResolution; i++) {
                Color gradientCol = gradient.Evaluate (i / (textureResolution - 1f));
                colours[i] = gradientCol;
            }

            texture.SetPixels (colours);
            texture.Apply ();
        }
    }
}
