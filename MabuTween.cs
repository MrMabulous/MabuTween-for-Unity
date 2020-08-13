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

/*
 * To tween anything, there are two static methods that can be used. 
 * Both return an IEnumerator, to be used as Coroutine.
 * If a property of any C# object shall be animated, then the following Method can be used:
 * 
 * TweenProperty(object targetObject,
 *         string propertyName,
 *         float timeInSeconds,
 *         T to,
 *         T? from = null,
 *         EasingFunction easingFunction = null,
 *         bool bounce = false,
 *         bool startThisFrameAlready = true);
 * 
 * The first 4 arguments must be specified, the others are optional.
 * @targetObject any C# reference type object that contains a property which should be animated.
 * @propertyName the name of the Property that should be animated
 * @timeInSeconds duration of the animation
 * @to the target value of the property that should be animated
 * @from if specified, that's the value the property will be set to at the beginning of the tween.
 *     otherwise it will tween starting at the value the property had when calling this function.
 * @easingFunction the easing function to use. If null, Sinosoid ease-in-ease-out is used. See
 *         https://easings.net/ for an overview of different easing types.
 * @bounce if false, the tween ends when the 'to' value is reached. if it is true, the tween
 *     animates back to the start value before it ends.
 * @startThisFrameAlready if true, the first step of the animation is already executed when the method
 *            is called. Otherwise the first step starts in the next frame.
 *            
 * Example usage:
 * 
 *         StartCoroutine(Tweener.TweenProperty(gameObject.transform, "position", 1.0f, Vector3.Up * 10));
 *         
 * This animates the global position of gameObject from it's current position to the
 * global position Vector3(0, 10, 0)
 *         
 *
 * 
 * 
 * If anything other than an object property should be animated (for example a value that can only be set
 * using a function call, or some Editor value or even some web api), then this more general Method can
 * be used:
 * 
 * Tween(SetterFunction<T> setterFunction,
 *     float timeInSeconds,
 *     T to,
 *     T? from = null,
 *     GetterFunction<T> getterFunction = null,
 *     EasingFunction easingFunction = null,
 *     bool bounce = false,
 *     bool startThisFrameAlready = true);
 *     
 * @timeInSeconds, @to, @easingFfunction, @bounce and @startThisFrameAlready have the same meaning as
 *         with TweenProperty(...)
 *     
 * @from has the same meaning as with TweenProperty(...), however, if it is null, then a @getterFunction
 *     must be provided. (meaning either @from or @getterFunction must be not null)
 * @setterFunction is a Delegate function that takes a single argument of type T (the type of the value that
 *         shall be animated) and returns void. This delegate function will be called repeatedly by
 *         the tweener with interpolated values. The implementation should simply set the vaue of
 *         whatever it is that should be animated.
 * @getterFunction is a Delegate function that takes no arguments and returns a value of type T. It is used
 *         by the tween to get the start value for the animation. if @from is not null, then
 *         @getterFunction is ignored.
 * 
 * 
 * Example usage: Let's say you have some function to set the visibility of the menu and you want to Tween it
 * 
 *          // Sets the alpha transparency of the Menu to @alpha
 *          public void SetUIVisiblity(float alpha) { ... }
 * 
 *          // Get the current alpha transparency of the menu
 *          public float GetUIVisibility();
 * 
 * To do so, you need to define a setter function. Since SetUIVisibility already conforms to the setterFunction
 * signature, and GetUIVisibility to the getterFunction signature, you can use them directly as delegate
 * 
 *          StartCoroutine(Tweener.Tween(SetUIVisiblity, 0.5f, 0.0f, null, GetUIVisibility));
 *
 * This will fade out the menu within 0.5 seconds using a Sinusoidal ease-in-ease-out animation. 
 * 
 * Alternatively you could also specify the @from value instead of providing the @getterFuncton:
 * 
 *          StartCoroutine(Tweener.Tween(SetUIVisiblity, 0.5f, 0.0f, 1.0f));
 *          
 * In this case the Menu will first be set to complete opacity (1.0f) if it isn't already, and then
 * faded out (0.0f)
 *          
 * 
 * Another example: Let's say you want to animate an increase in distance between two objects.
 * To do so, define a setterFunction (this can be an anonymous lambda function, like in this example):
 * 
 *         SetterFunction<float> distanceSetter = (float d) => {
 *           Vector3 center = (go1.transform.position + go2.transform.position) * 0.5f;
 *           go1.transform.position = center + (go1.transform.position - center).normalized * d * 0.5f;
 *           go2.transform.position = center + (go2.transform.position - center).normalized * d * 0.5f;
 *         }
 *         float currentDistance = (go1.transform.position - go2.transform.position).magnitude;
 *         StartCoroutine(Tweener.Tween(distanceSetter, 2.0f, 10.0f, currentDistance, null, Easing.Bounce.Out));
 * 
 * This will create a 10 seconds animation that will make the two GameObjects move to a distance of 10.0 units from
 * each other.
 *          
 * 
 * On the Type T:
 * By default, values of types float, double, Vector2, Vector3, Vector4, Quaternion and Color can be animated, as
 * well as any other Value Type that has a static method T Lerp(T a, T b, float t) defined.
 * 
 * However, to animate any Value Type variable can be added by simply defining a suitable Lerp function and setting
 * it once using the static SetLerpMethod() function.
 * 
 * For example, to make variables of type int tweenable, one could subscribe a Lerp function for it in the following
 * way:
 * 
 *           Func<int, int, float, int> lerpInt = (int a, int b, float t) => { 
 *             return (int)(a + ((float)b - a) * Mathf.Clamp01(t));
 *           };
 *           Tweener.SetLerpMethod(typeof(int), lerpInt);
 * 
 * After this, variables of type int can be animated using Tween(...); and TweenProperty(...);
*/

using System;
using System.Reflection;
using System.Linq;
using System.Linq.Expressions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MabuTween
{
  public delegate T GetterFunction<T>() where T : struct;
  public delegate void SetterFunction<T>(T x) where T : struct;

  public static class Tweener
  {
    private static Dictionary<Type, Delegate> lerpMethodDic = new Dictionary<Type, Delegate>();

    static Tweener()
    {
      // Register lerpMethods for float and double.
      SetLerpMethod(typeof(float), (Func<float,float,float,float>)((float a, float b, float t) => { return Mathf.Lerp(a, b, t); }));
      SetLerpMethod(typeof(double), (Func<double, double, float, double>)((double a, double b, float t) => { return a + (b - a) * Mathf.Clamp01(t); }));
    }

    // Define a Lerp method in order to animate other types of variables than the ones supported by default.
    public static void SetLerpMethod(Type type, Delegate lerpMethod)
    {
      Type delegateType = Expression.GetFuncType(new[] { type, type, typeof(float), type });
      if (!lerpMethod.GetType().Equals(delegateType))
      {
        Debug.LogError("Invalid Lerp Method Delegate for type " + type.FullName + " . expected " + delegateType.FullName + " but got " + lerpMethod.GetType().FullName);
      }
      lerpMethodDic[type] = lerpMethod;
    }

    // Tween anything.
    public static IEnumerator Tween<T>(SetterFunction<T> setterFunction, float timeInSeconds, T to, T? from = null, GetterFunction<T> getterFunction = null, EasingFunction easingFunction = null, bool bounce = false, bool startThisFrameAlready = true) where T : struct
    {
      // Perform runtime checks whether the property can be tweened.
      if (setterFunction == null)
      {
        Debug.LogWarning("setterFunction is null. Will not tween.");
        yield break;
      }
      if (getterFunction == null && setterFunction == null)
      {
        Debug.LogWarning("either 'from' or getterFunction must be non-null. Will not tween.");
        yield break;
      }
      // search in lerpMethodDic
      Delegate del;
      lerpMethodDic.TryGetValue(typeof(T), out del);
      Func<T, T, float, T> lerpMethod = del as Func<T, T, float, T>;
      if (lerpMethod == null)
      {
        // Check if type has a static Lerp function and create a Delegate for it if it has.
        MethodInfo lerpMethodInfo = typeof(T).GetMethod("Lerp", BindingFlags.Static | BindingFlags.Public);
        Type delegateType = Expression.GetFuncType(new[] { typeof(T), typeof(T), typeof(float), typeof(T) });
        if (lerpMethodInfo != null)
        {
          lerpMethod = Delegate.CreateDelegate(delegateType, lerpMethodInfo) as Func<T, T, float, T>;
          lerpMethodDic[typeof(T)] = lerpMethod;
        }
        else
        {
          Debug.LogWarning("Type " + typeof(T).FullName + " has no static Lerp() function defined. Set a Lerp function of type System.Func<" + typeof(T).Name + "," + typeof(T).Name + ",float," + typeof(T).Name + "> using SetLerpFunction().");
          yield break;
        }
      }

      // Use sinusoidal easing if no easing function has been provided
      if (easingFunction == null)
        easingFunction = Easing.Sinusoidal.InOut;

      // Get the current value of the property.
      T fromValue;
      if (from != null)
      {
        fromValue = (T)from;
      } else
      {
        // Checked before that getterFunction is non-null in this case.
        fromValue = getterFunction();
      }
      float tweenTime = 0;

      if (!startThisFrameAlready)
      {
        yield return 0;
      }

      float t;
      do
      {
        tweenTime += Time.deltaTime;
        t = tweenTime / timeInSeconds;
        float tweenValue = easingFunction(Mathf.Clamp(t, 0, 1));
        T newValue = lerpMethod(fromValue, to, tweenValue);
        setterFunction(newValue);
        yield return 0;
      } while (t < 1.0f);
      if(bounce)
      {
        // animate back.
        do
        {
          tweenTime -= Time.deltaTime;
          t = tweenTime / timeInSeconds;
          float tweenValue = easingFunction(Mathf.Clamp(t, 0, 1));
          T newValue = lerpMethod(fromValue, to, tweenValue);
          setterFunction(newValue);
          yield return 0;
        } while (t > 0.0f);
      }
    }

    // Tween a property on some object.
    public static IEnumerator TweenProperty<U, T>(U targetObject, string propertyName, float timeInSeconds, T to, T? from = null, EasingFunction easingFunction = null, bool bounce = false, bool startThisFrameAlready = true) where U : class where T : struct
    {
      // Perform runtime checks whether the property can be tweened.
      if (!targetObject.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Any(p => p.Name == propertyName))
      {
        Debug.LogWarning("Object " + targetObject.ToString() + " does not contain a property named " + propertyName + ". Will not tween.");
        yield break;
      }
      PropertyInfo propInfo = targetObject.GetType().GetProperty(propertyName);
      Type propType = propInfo.PropertyType;
      if (!propType.Equals(typeof(T)))
      {
        Debug.LogWarning("Property " + propertyName + " on object " + targetObject.ToString() + " is of type " + propType.FullName + " but 'to' is of type " + to.GetType().FullName + ". Will not tween.");
        yield break;
      }
      yield return Tween((T x) => { propInfo.SetValue(targetObject, x); }, timeInSeconds, to, from, () => { return (T)propInfo.GetValue(targetObject); }, easingFunction, bounce, startThisFrameAlready);      
    }
  }
}  // namespace MabuTween