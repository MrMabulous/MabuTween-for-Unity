/*
The MIT License

Copyright(c) 2020 Matthias Bühlmann, Mabulous GmbH.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MabuTween;

// Place this on a default Cube.

public class MabuTweenTest : MonoBehaviour
{
  // Start is called before the first frame update
  void Start()
  {
    StartCoroutine(TestTweens());
  }

  IEnumerator TestTweens()
  {
    // repeat forever
    while (true)
    {
      // smooth motion
      yield return Tweener.TweenProperty(transform, "position", 1.0f, new Vector3(0, 1, 0));
      yield return Tweener.TweenProperty(transform, "position", 1.0f, new Vector3(1, 1, 0));
      yield return Tweener.TweenProperty(transform, "position", 1.0f, new Vector3(1, 1, 1));
      yield return Tweener.TweenProperty(transform, "position", 1.0f, new Vector3(0, 0, 0));
      yield return new WaitForSeconds(0.3f);

      // jerky motion
      yield return Tweener.TweenProperty(transform, "position", 0.5f, new Vector3(0, 1, 0), null, Easing.Bounce.Out);
      yield return Tweener.TweenProperty(transform, "position", 0.5f, new Vector3(1, 1, 0), null, Easing.Bounce.Out);
      yield return Tweener.TweenProperty(transform, "position", 0.5f, new Vector3(1, 1, 1), null, Easing.Bounce.Out);
      yield return Tweener.TweenProperty(transform, "position", 0.5f, new Vector3(0, 0, 0), null, Easing.Bounce.Out);
      yield return new WaitForSeconds(0.3f);

      // animate orientation
      Quaternion current_rot = transform.rotation;
      Quaternion to = Quaternion.Euler(60, 30, 90);
      yield return Tweener.TweenProperty(transform, "rotation", 1.0f, to, null, Easing.Bounce.Out);
      yield return Tweener.TweenProperty(transform, "rotation", 1.0f, current_rot, null, Easing.Bounce.Out);
      yield return new WaitForSeconds(0.3f);

      // animate from
      float squash = Mathf.Sqrt(0.333f); //volume preserving defomr
      yield return Tweener.TweenProperty(transform, "localScale", 1.0f, Vector3.one, new Vector3(squash, 3, squash), Easing.Cubic.Out);
      yield return Tweener.TweenProperty(transform, "localScale", 1.0f, Vector3.one, new Vector3(3, squash, squash), Easing.Cubic.Out);
      yield return Tweener.TweenProperty(transform, "localScale", 1.0f, Vector3.one, new Vector3(squash, squash, 3), Easing.Cubic.Out);
      yield return new WaitForSeconds(0.3f);

      // animate material properties
      Material material = gameObject.GetComponent<Renderer>().material;
        
      // animate alpha
      // specify custom setter function for the variable you want to animate.
      SetterFunction<float> alphaSetter = (float a) => {
        Color col = material.GetColor("_Color");
        col.a = a;
        gameObject.GetComponent<Renderer>().material.SetColor("_Color", col);
      };
      yield return Tweener.Tween(alphaSetter, 1.0f, 0.0f, material.GetColor("_Color").a, null, Easing.Quadratic.Out, true); // fade out and back in (bounce = true)
      yield return new WaitForSeconds(0.3f);
        
      // animate color
      // specify custom setter function for the variable you want to animate.
      SetterFunction<Color> colorsetter = (Color col) => {
        gameObject.GetComponent<Renderer>().material.SetColor("_Color", col);
      };
      Color current_color = material.GetColor("_Color");
      yield return Tweener.Tween(colorsetter, 1.0f, Color.red, current_color); // fade to red
      yield return Tweener.Tween(colorsetter, 1.0f, Color.blue, Color.red); // fade to blue
      yield return Tweener.Tween(colorsetter, 1.0f, current_color, Color.blue); // fade to previous color
      yield return new WaitForSeconds(0.3f);

      // animate some vertices
      // specify custom setter function that sets multiple vertices.
      Mesh mesh = gameObject.GetComponent<MeshFilter>().mesh;
      Vector3[] vertices_original = mesh.vertices;
      Vector3 target = transform.position + transform.up * 2;
      List<int> indices_of_upper_vertices = new List<int>();
      for (int i = 0; i < vertices_original.Length; i++) { if (vertices_original[i].y > 0) indices_of_upper_vertices.Add(i); }
      SetterFunction<float> vertexSetter = (float t) =>
      {
        Vector3[] vertices = mesh.vertices;
        for (int i=0; i< indices_of_upper_vertices.Count; i++) // move upper vertices towards target
        {
          int idx = indices_of_upper_vertices[i];
          vertices[idx] = Vector3.Lerp(vertices_original[idx], target, t);
          mesh.vertices = vertices;
        }
      };
      yield return Tweener.Tween(vertexSetter, 1.0f, 1.0f, 0.0f, null, Easing.Quadratic.InOut, true); // move the vertices and bounce back.

    }
  }
}
