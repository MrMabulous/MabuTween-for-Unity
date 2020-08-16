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
    // general usage is: Tween(what, duration, to, [from], [easingFunction], [loopType], [getterFunction])
    //
    // @what defines the value that should be animated. It can can either be a tuple (object, propertyname) or a
    //       setter function of type SetterFunction<T> that takes one argument of type T (in the range of @from to @to)
    //       and does something with it.
    // @duration is the duration of the tween in seconds.
    // @to is the target value to which the value should be animated.
    // @from defines the value at which the animation should start. It can take one of three types:
    //       - a value of type T, in this case that value will be set as the start
    //       - null, in which case the start value is taken from the value the property will have when the tween is
    //               started. This only works when @what is an (object, propertyname) tuple, not when it is a
    //               setter function of type SetterFunction<T>
    //       - a getter function of type GetterFunction<T>. It will be evaluated when the tween starts to get the start
    //               value.
    // @easingFunction one of many different easing functions defining motion curve. See https://easings.net/
    //                 can be null, in which case sinusoidal ease-in-ease-out is used.
    // @loopType defines what happens when the tween reaches the end. Possible Values are
    //           - LoopType.Dont (the default): doesn't loop, the tween ends when it reaches the to value.
    //           - LoopType.Loop: repeats the tween forwever, starting at the beginning with each loop
    //           - LoopType.Reflect: After reaching the end, animates in reverse back to the start and ends then.
    //           - LoopType.PingPong: Animates continuously forwards, then backwards etc.
    // @getterFunction this is usually not required. it is only required when the @what of a subtween is defined using a
    //                 setter function rather than a (object, propertyname) tuple AND the @from is NOT already of type
    //                 GetterFunction<T> but a fixed value of type T AND the subtween is concatenated with other
    //                 subtweens and might animated backwards (this is so the tween can query the value of the animated
    //                 value before the tween starts, so that it can set it back there when animating in reverse).

    // build one long tween by creating and concatinating many individual tweens using + and += operator.

    // smooth motion
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
    Color current_color = material.color;
    tween += Mabu.Tween((material, "color"), 1.0f, Color.red) +      // fade to red
             Mabu.Tween((material, "color"), 1.0f, Color.blue) +     // fade to blue
             Mabu.Tween((material, "color"), 1.0f, current_color) +  // fade to previous color
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
