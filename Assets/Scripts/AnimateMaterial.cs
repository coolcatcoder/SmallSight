using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimateMaterial : MonoBehaviour
{
    [SerializeField] private Texture2D[] frames;
    [SerializeField] private float fps = 10.0f;

    public Material mat;

    IEnumerator Start()
    {
        int i = 0;
        float PreDivision = 1f / fps;
        while(true)
        {
            yield return new WaitForSeconds(PreDivision);
            i++;
            mat.mainTexture = frames[i];
            if (i == frames.Length - 1)
            {
                i = -1;
            }
        }
    }

    //void Update()
    //{
    //    //int index = (int)(Time.time * fps);
    //    //index = index % frames.Length;
    //    //mat.mainTexture = frames[index];
    //}
}
