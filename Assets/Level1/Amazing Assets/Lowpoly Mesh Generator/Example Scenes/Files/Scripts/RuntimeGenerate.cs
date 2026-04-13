using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace AmazingAssets.LowpolyMeshGenerator.Examples
{
    public class RuntimeGenerate : MonoBehaviour
    {
        public AmazingAssets.LowpolyMeshGenerator.SamplingType samplingType;
        public AmazingAssets.LowpolyMeshGenerator.SourceVertexColor sourceVertexColor;

        public bool mergeFullHierarchy = false;

        public Material material;


        void Start()
        {
            //Generate lowpoly style meshes for all MeshFilters in this.gameobject hierarchy and replace them.                                                                 

            List<Mesh> lowpolyMeshes = new List<Mesh>();
            foreach (MeshFilter meshFilter in gameObject.GetComponentsInChildren<MeshFilter>())
            {
                if(meshFilter != null && meshFilter.sharedMesh != null)
                {
                    //Collect textures and colors from used materials

                    Texture2D[] bakeTextures = null;
                    Color[] bakeColors = null;

                    Renderer renderer = meshFilter.gameObject.GetComponent<Renderer>();
                    if(renderer != null && renderer.sharedMaterials != null && renderer.sharedMaterials.Length > 0)
                    {
                        bakeTextures = new Texture2D[renderer.sharedMaterials.Length];
                        bakeColors = new Color[renderer.sharedMaterials.Length];

                        //Read bake texture and color properties from material
                        for (int i = 0; i < renderer.sharedMaterials.Length; i++)
                        {
                            bakeTextures[i] = (Texture2D)renderer.sharedMaterials[i].mainTexture;
                            bakeColors[i] = renderer.sharedMaterials[i].color;
                        }
                    }



                    //Generating lowpoly mesh

                    Mesh newMesh = meshFilter.sharedMesh.GenerateLowpolyMesh(bakeTextures, null, bakeColors, 0, samplingType, sourceVertexColor, AlphaType.DefaultValue, true);
                    if (newMesh != null)
                    {
                        lowpolyMeshes.Add(newMesh);

                        //Replace mesh
                        meshFilter.sharedMesh = newMesh;

                        //Replace material
                        if (renderer != null)
                            renderer.sharedMaterials = new Material[] { material };
                    }
                }
            }








            //Combine all meshes in this.gameobject into ONE mesh
            if (lowpolyMeshes.Count > 0 && mergeFullHierarchy == true)
            {       
                this.gameObject.CombineAllChildren(material, "Combined");


                //After combining meshes into ONE, we do not need old lowpoly meshes
                for (int i = 0; i < lowpolyMeshes.Count; i++)
                {
                    lowpolyMeshes[i].Clear();
                }
                lowpolyMeshes = null;
            }
        }
    }
}