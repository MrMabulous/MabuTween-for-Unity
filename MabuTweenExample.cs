/*
Copyright(c) 2020 Matthias Bühlmann, Mabulous GmbH. http://www.mabulous.com

All rights reserved.
*/

using System.Collections.Generic;
using UnityEngine;

// Place this on a default Cube with a material having 'Transparent' Rendering Mode.

public class MabuTweenExample : MonoBehaviour
{
  // Start is called before the first frame update
  void Start()
  {
    // build one long tween by creating and concatinating many individual tweens using + and += operator.
    Mabu.TweenHandle tween = Mabu.Tween((transform, "position"), 1.0f, new Vector3(0, 1, 0)) +
                             Mabu.Tween((transform, "position"), 1.0f, new Vector3(1, 1, 0)) +
                             Mabu.Tween((transform, "position"), 1.0f, new Vector3(1, 1, 1)) +
                             Mabu.Tween((transform, "position"), 1.0f, new Vector3(0, 0, 0)) +
                             new WaitForSeconds(0.3f); // you can concatenate YieldInstructions too.

    // jerky motion
    tween += Mabu.Tween((transform, "position"), 0.5f, new Vector3(0, 1, 0), null, Mabu.Easing.Bounce.Out) +
             Mabu.Tween((transform, "position"), 0.5f, new Vector3(1, 1, 0), null, Mabu.Easing.Bounce.Out) +
             Mabu.Tween((transform, "position"), 0.5f, new Vector3(1, 1, 1), null, Mabu.Easing.Bounce.Out) +
             Mabu.Tween((transform, "position"), 0.5f, new Vector3(0, 0, 0), null, Mabu.Easing.Bounce.Out) +
             new WaitForSeconds(0.3f);

    // animate orientation
    Quaternion current_rot = transform.rotation;
    Quaternion to = Quaternion.Euler(60, 30, 90);
    tween += Mabu.Tween((transform, "rotation"), 1.0f, to, null, Mabu.Easing.Bounce.Out) +
             Mabu.Tween((transform, "rotation"), 1.0f, current_rot, null, Mabu.Easing.Bounce.Out) +
             new WaitForSeconds(0.3f);

    // animate from
    float squash = Mathf.Sqrt(0.333f); //volume preserving defom
    tween += Mabu.Tween((transform, "localScale"), 1.0f, Vector3.one, new Vector3(squash, 3, squash),
                        Mabu.Easing.Cubic.Out) +
             Mabu.Tween((transform, "localScale"), 1.0f, Vector3.one, new Vector3(3, squash, squash),
                        Mabu.Easing.Cubic.Out) +
             Mabu.Tween((transform, "localScale"), 1.0f, Vector3.one, new Vector3(squash, squash, 3),
                        Mabu.Easing.Cubic.Out) +
             new WaitForSeconds(0.3f);

    // animate material properties
    Material material = gameObject.GetComponent<Renderer>().material;

    // animate alpha
    // specify custom setter function for the variable you want to animate.
    Mabu.SetterFunction<float> alphaSetter = (float a) => {
      Color col = material.GetColor("_Color");
      col.a = a;
      material.SetColor("_Color", col);
    };
    // fade out and back in (LoopType.Reflect)
    tween += Mabu.Tween(alphaSetter, 1.0f, 0.0f, 1.0f, Mabu.Easing.Quadratic.Out, Mabu.LoopType.Reflect) +
             new WaitForSeconds(0.3f);
        
    // animate color
    // specify custom setter function for the variable you want to animate.
    Mabu.SetterFunction<Color> colorsetter = (Color col) => {
      gameObject.GetComponent<Renderer>().material.SetColor("_Color", col);
    };
    Mabu.GetterFunction<Color> colorgetter = () => {
      return gameObject.GetComponent<Renderer>().material.GetColor("_Color");
    };
    Color current_color = colorgetter();
    tween += Mabu.Tween(colorsetter, 1.0f, Color.red, colorgetter) +  // fade to red
              Mabu.Tween(colorsetter, 1.0f, Color.blue, colorgetter) +  // fade to blue
              Mabu.Tween(colorsetter, 1.0f, current_color, colorgetter) +  // fade to previous color
              new WaitForSeconds(0.3f);

    // animate some vertices
    // specify custom setter function that sets multiple vertices.
    Mesh mesh = gameObject.GetComponent<MeshFilter>().mesh;
    Vector3[] vertices_original = mesh.vertices;
    Vector3 target = transform.position + transform.up * 2;
    List<int> indices_of_upper_vertices = new List<int>();
    for (int i = 0; i < vertices_original.Length; i++) 
    { 
      if (vertices_original[i].y > 0) indices_of_upper_vertices.Add(i);
    }
    Mabu.SetterFunction<float> vertexSetter = (float t) =>
    {
      Vector3[] vertices = mesh.vertices;
      for (int i=0; i< indices_of_upper_vertices.Count; i++)  // move upper vertices towards target.
      {
        int idx = indices_of_upper_vertices[i];
        vertices[idx] = Vector3.Lerp(vertices_original[idx], target, t);
        mesh.vertices = vertices;
      }
    };
    // move the vertices and bounce back (LoopType.Reflect).
    tween += Mabu.Tween(vertexSetter, 1.0f, 1.0f, 0.0f, Mabu.Easing.Quadratic.InOut, Mabu.LoopType.Reflect);
    
    tween.LoopType = Mabu.LoopType.PingPong;
  }
}
