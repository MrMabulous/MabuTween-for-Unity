/*
Copyright(c) 2020 Matthias Bühlmann, Mabulous GmbH. http://www.mabulous.com

All rights reserved.
*/

/*
 * To tween anything, there are two static methods that can be used. 
 * Both return an IEnumerator, to be used as Coroutine.
 * If a property of any C# object shall be animated, then the following Method can be used:
 * 
 * Tween(Tuple<object, string> targetProperty,
 *       float timeInSeconds,
 *       T to,
 *       object from = null,
 *       EasingFunction easingFunction = null,
 *       LoopType loopType = LoopType.Dont);
 * 
 * The first 3 arguments must be specified, the others are optional.
 * @targetObject a tuple consisting of a reference to the c# object containing the property
 *               to be animated plus the name of the property.
 * @propertyName the name of the Property that should be animated.
 * @timeInSeconds duration of the animation.
 * @to the target value of the property that should be animated
 * @from if specified, that's the value the property will be set to at the beginning of the tween.
 *     otherwise it will tween starting at the value the property had when calling this function.
 * @easingFunction the easing function to use. If null, Sinosoid ease-in-ease-out is used. See
 *         https://easings.net/ for an overview of different easing types.
 * @loopType if false, the tween ends when the 'to' value is reached. if it is true, the tween
 *     animates back to the start value before it ends.
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
 *       float timeInSeconds,
 *       T to,
 *       T from,
 *       EasingFunction easingFunction = null,
 *       LoopType loopType = LoopType.Dont);
 *     
 * @timeInSeconds, @to, @easingFfunction, @loopType and @startThisFrameAlready have the same meaning as
 *         with TweenProperty(...)
 *     
 * @from has the same meaning as with TweenProperty(...), however, it cannot be null.
 * @setterFunction is a Delegate function that takes a single argument of type T (the type of the value that
 *         shall be animated) and returns void. This delegate function will be called repeatedly by
 *         the tweener with interpolated values. The implementation should simply set the vaue of
 *         whatever it is that should be animated.
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
 * This will create a 10 seconds animation that will Tween the two GameObjects move to a distance of 10.0 units from
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
 * For example, to Tween variables of type Rect tweenable, one could subscribe a Lerp function for it in the following
 * way:
 * 
 *           LerpFunction<Rect> lerpRect = (Rect a, Rect b, float t) => { 
 *             return Rect.MinMaxRect(Mathf.Lerp(a.xMin, b.xMin, t),
 *                                    Mathf.Lerp(a.yMin, b.yMin, t),
 *                                    Mathf.Lerp(a.xMax, b.xMax, t),
 *                                    Mathf.Lerp(a.yMax, b.yMax, t),
 *           };
 *           Mabu.SetLerpMethod(typeof(Rect), lerpRect);
 * 
 * After this, variables of type Rect can be animated using Tween(...); and TweenProperty(...);
*/

using System;
using System.Reflection;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Mabu
{
  public delegate void SetterFunction<T>(T x) where T : struct;
  public delegate T GetterFunction<T>() where T : struct;
  public delegate T LerpFunction<T>(T a, T b, float t) where T : struct;
  public delegate float EasingFunction(float k);

	public enum LoopType
  {
		Dont,    // End the tween after playing once.
		Loop,    // Loops continuously.
    Reflect, // Plays one time forward, then one time Reverse, then is over.
		PingPong // Plays continuously forward then Reverse.
	}

  public enum TweenPlayDirection
  {
    Forward = 1,
    Reverse = -1
  }

  public static TweenHandle Tween<T>(SetterFunction<T> setterFunction, float timeInSeconds, T to, object from, EasingFunction easingFunction = null, LoopType loopType = LoopType.Dont, GetterFunction<T> getterFunction = null) where T : struct
  {
    return new EnumeratorTween(new RawLoopableTween<T>(setterFunction, timeInSeconds, to, from, easingFunction, loopType, getterFunction));
  }

  public static TweenHandle Tween<U,T>(ValueTuple<U, string> targetProperty, float timeInSeconds, T to, object from = null, EasingFunction easingFunction = null, LoopType loopType = LoopType.Dont) where U : class where T : struct
  {
    return new EnumeratorTween(new RawLoopableTween<T>(targetProperty, timeInSeconds, to, from, easingFunction, loopType));
  }

  public static TweenHandle Tween<T>(object[] targetProperty, float timeInSeconds, T to, object from = null, EasingFunction easingFunction = null, LoopType loopType = LoopType.Dont) where T : struct
  {
    return new EnumeratorTween(new RawLoopableTween<T>(targetProperty, timeInSeconds, to, from, easingFunction, loopType));
  }

#region CustomLerpMethods

  private static Dictionary<Type, Delegate> lerpMethodDic = new Dictionary<Type, Delegate>();

  static Mabu()
  {
    // Register lerpMethods for float and double.
    SetLerpMethod((float a, float b, float t) => { return Mathf.Lerp(a, b, t); });
    SetLerpMethod((double a, double b, float t) => { return a + (b - a) * Mathf.Clamp01(t); });
  }

  // Define a Lerp method in order to animate other types of variables than the ones supported by default.
  public static void SetLerpMethod<T>(LerpFunction<T> lerpMethod) where T : struct
  {
    Type type = typeof(T);
    if (lerpMethod == null)
    {
      Debug.LogError("LerpMethod is null");
    }
    lerpMethodDic[type] = lerpMethod;
  }

#endregion

#region TweenManager

  private static class TweenManager
  {
    private class CoroutineStarter : MonoBehaviour { }

    private static GameObject tweenManagerGO;
    private static Dictionary<TweenHandle, Coroutine> coroutines = new Dictionary<TweenHandle, Coroutine>();
    private static List<TweenHandle> deadTweens = new List<TweenHandle>();
    private static CoroutineStarter coroutineStarter;

    static TweenManager()
    {
      tweenManagerGO = new GameObject("TweenManagerGO");
      tweenManagerGO.hideFlags = HideFlags.HideAndDontSave | HideFlags.HideInInspector;
      coroutineStarter = tweenManagerGO.AddComponent<CoroutineStarter>();
    }

    public static void AddTween(TweenHandle tween)
    {
      if(tween != null)
        coroutines[tween] = coroutineStarter.StartCoroutine(CoroutineWrapper(tween));
    }

    public static void RemoveTween(TweenHandle tween)
    {
      if(tween != null) {
        Coroutine coroutine;
        if(coroutines.TryGetValue(tween, out  coroutine)) {
          coroutineStarter.StopCoroutine(coroutine);
          coroutines.Remove(tween);
        }
      }
    }

    private static IEnumerator CoroutineWrapper(TweenHandle tween)
    {
      // StartCoroutine already executes one iteration, therefore
      // one initial yield return 0 is inserted so that the actual
      // tween does not start yet, so that it still can be removed
      // from the manager during this frame without affect the
      // object, like setting the 'from' value.
      yield return 0;
      while(true)
      {
        bool moved = tween.MoveNext();
        if(moved)
        {
          yield return tween.Current;
        }
        else
        {
          RemoveTween(tween);
          yield break;
        }
      }
    }
  }

#endregion

#region ReversableEnumerator

  public interface IReversableEnumerator : IEnumerator
  {
    bool MovePrevious();
    void Reset(TweenPlayDirection direction);
    bool Move(TweenPlayDirection direction);
  }

  public abstract class ReversableEnumerator : IReversableEnumerator
  {
    public virtual bool MoveNext() { return Move(TweenPlayDirection.Forward); }
    public virtual bool MovePrevious() { return Move(TweenPlayDirection.Reverse); }
    public virtual object Current { get { return Inner.Current; } }
    public virtual void Reset() { Reset(TweenPlayDirection.Forward); }

    // After reset in direction, a Move in the same direction will not return false, while one
    // in the opposite direction will.
    public abstract void Reset(TweenPlayDirection direction);
    public abstract bool Move(TweenPlayDirection direction);
    protected IEnumerator Inner { get { return GetInner(); } }
    protected abstract IEnumerator GetInner();
  }
#endregion

#region LoopableEnumerator
  public interface ILoopableEnumerator : IReversableEnumerator
  {
    LoopType LoopType { get; set; }
  }

  public abstract class LoopableEnumerator :  ReversableEnumerator, ILoopableEnumerator
  {
    public LoopType LoopType { get; set; } = LoopType.Dont;
    private bool loopingBack = false;

    public override void Reset(TweenPlayDirection direction)
    {
      TweenPlayDirection actualDir = direction;
      if(LoopType == LoopType.PingPong || LoopType == LoopType.Reflect)
      {
        loopingBack = actualDir == TweenPlayDirection.Reverse;
        actualDir = TweenPlayDirection.Forward;
      } else
      {
        loopingBack = false;
      }
      Inner.Reset(actualDir);
    }

    public override bool Move(TweenPlayDirection dir)
    {
      TweenPlayDirection actualDirection = (TweenPlayDirection)((int)dir * (loopingBack ? -1 : 1));
      bool moved = Inner.Move(actualDirection);
      if(!moved)
      {
        switch(LoopType)
        {
        case LoopType.Loop:
          Inner.Reset(actualDirection);
          moved = Inner.Move(actualDirection);
          break;
        case LoopType.Reflect:
          if(loopingBack == (dir == TweenPlayDirection.Reverse))
          {
            goto case LoopType.PingPong;
          }
          break;
        case LoopType.PingPong:
          loopingBack = !loopingBack;
          actualDirection = (TweenPlayDirection)((int)actualDirection * -1);
          // reset here too, so that YieldInstruction Subtweens also run again
          Inner.Reset(actualDirection);
          moved = Inner.Move(actualDirection);
          break;
        default:
          // ending.
          break;
        }
      }
      return moved;
    }

    protected new IReversableEnumerator Inner { get { return (IReversableEnumerator)GetInner(); } }
  }

#endregion

#region Tween

  public abstract class TweenHandle : LoopableEnumerator
  {
    public static TweenHandle operator +(TweenHandle first, TweenHandle second)
        => first.Then(second);

    public static TweenHandle operator +(TweenHandle first, YieldInstruction second)
        => first.Then(new EnumeratorTween(new PseudoReversableEnumerator(new YieldInstructionEnumerator(second))));

    public TweenHandle Then(TweenHandle next)
    {
      return new ChainedTween(this, next);
    }
  }

  private class PseudoReversableEnumerator : ReversableEnumerator
  {
    // Wraps a regular IEnumerator and iterating always forward
    // independent of direction. While this is incorrect, it's
    // the best we can do to with such enumerators.
    private IEnumerator inner;
    public PseudoReversableEnumerator(IEnumerator other)
    {
      inner = other;
    }

    public override bool Move(TweenPlayDirection direction) { return inner.MoveNext(); }

    public override void Reset(TweenPlayDirection direction)
    {
      inner.Reset();
    }

    protected override IEnumerator GetInner() { return inner; }
  }

  private class YieldInstructionEnumerator : IEnumerator
  {
    private YieldInstruction yi;
    bool moveNextCalled = false;

    public YieldInstructionEnumerator(YieldInstruction instruction)
    {
      yi = instruction;
    }

    public bool MoveNext()
    {
      bool res = !moveNextCalled;
      moveNextCalled = true;
      return res;
    }

    public object Current { get { return moveNextCalled ? yi : null; } }

    public void Reset() { moveNextCalled = false; }
  }

  private class EnumeratorTween : TweenHandle
  {
    private IReversableEnumerator inner;

    public EnumeratorTween(IReversableEnumerator en) {
      inner = en;
      TweenManager.AddTween(this);
    }

    protected override IEnumerator GetInner() { return inner; }
  }

  private class CompoundReversableEnumerator : IReversableEnumerator
  {
    private IReversableEnumerator current;
    private IReversableEnumerator first;
    private IReversableEnumerator second;

    public CompoundReversableEnumerator(IReversableEnumerator first, IReversableEnumerator second)
    {
      this.first = first;
      current = first;
      this.second = second;
    }

    public virtual bool MoveNext() { return Move(TweenPlayDirection.Forward); }
    public virtual bool MovePrevious() { return Move(TweenPlayDirection.Reverse); }
    public virtual void Reset() { Reset(TweenPlayDirection.Forward); }
    public virtual void Reset(TweenPlayDirection dir)
    {
      first.Reset(dir);
      second.Reset(dir);
      if(dir == TweenPlayDirection.Forward)
      {
        current = first;
      }
      else
      {
        current = second;
      }
    }
    
    public virtual object Current { get { return current.Current; } }

    public virtual bool Move(TweenPlayDirection dir)
    {
      bool moved = current.Move(dir);
      if(!moved)
      {
        if((dir == TweenPlayDirection.Reverse && current == first) ||
           (dir == TweenPlayDirection.Forward && current == second))
        {
          return false;
        }
        else
        {
          if(current == first)
          {
            current = second;
          }
          else
          {
            current = first;
          }
          moved = current.Move(dir);
        }
      }
      return moved;
    }
  } 

  private class ChainedTween : TweenHandle
  {
    private CompoundReversableEnumerator inner;

    public ChainedTween(TweenHandle first, TweenHandle second)
    {
      inner = new CompoundReversableEnumerator(first, second);
      TweenManager.RemoveTween(first);
      TweenManager.RemoveTween(second);
      TweenManager.AddTween(this);
    }

    protected override IEnumerator GetInner() { return inner; }
  }
#endregion

#region RawTween
  // Helper class to return an empty IEnumerator
  private class EmptyEnumerator : IEnumerator
  {
    public bool MoveNext() { return false; }
    public object Current { get { return null; } }
    public void Reset() { }
  }

  public class RawLoopableTween<T> : LoopableEnumerator where T : struct
  {
    private RawTween<T> inner;

    public RawLoopableTween(SetterFunction<T> setterFunction, float timeInSeconds, T to, object from, EasingFunction easingFunction = null, LoopType loopType = LoopType.Dont, GetterFunction<T> getterFunction = null)
    {
      LoopType = loopType;
      inner = new RawTween<T>(setterFunction, timeInSeconds, to, from, easingFunction, TweenPlayDirection.Forward, getterFunction);
    }

    public RawLoopableTween(object[] targetProperty, float timeInSeconds, T to, object from = null, EasingFunction easingFunction = null, LoopType loopType = LoopType.Dont)
    {
      LoopType = loopType;
      inner = new RawTween<T>(targetProperty, timeInSeconds, to, from, easingFunction, TweenPlayDirection.Forward);
    }

    public RawLoopableTween(ValueTuple<object, string> targetProperty, float timeInSeconds, T to, object from = null, EasingFunction easingFunction = null, LoopType loopType = LoopType.Dont)
    {
      LoopType = loopType;
      inner = new RawTween<T>(targetProperty, timeInSeconds, to, from, easingFunction, TweenPlayDirection.Forward);
    }

    protected override IEnumerator GetInner() { return inner; }
  }

  public class RawTween<T> : ReversableEnumerator where T : struct
  {
    private enum RawTweenState
    {
      Running,
      LeftEnd,
      RightEnd,
      Resetting
    }

    private IEnumerator inner;
    protected override IEnumerator GetInner() { return inner; }
    private RawTweenState state = RawTweenState.LeftEnd;
    private TweenPlayDirection resetDirection = TweenPlayDirection.Forward;
    private TweenPlayDirection currentPlayDirection = TweenPlayDirection.Forward;

    public override bool Move(TweenPlayDirection dir)
    {
      currentPlayDirection = dir;
      if((state == RawTweenState.RightEnd && currentPlayDirection == TweenPlayDirection.Forward) ||
         (state == RawTweenState.LeftEnd && currentPlayDirection == TweenPlayDirection.Reverse))
      {
        return false;
      }
      return inner.MoveNext();
    }

    public override void Reset(TweenPlayDirection dir)
    {
      resetDirection = dir;
      state = RawTweenState.Resetting;
    }

    public RawTween(SetterFunction<T> setterFunction, float timeInSeconds, T to, object from, EasingFunction easingFunction = null, TweenPlayDirection playDirection = TweenPlayDirection.Forward, GetterFunction<T> getterFunction = null)
    {
      inner = RawTweenImpl(setterFunction, timeInSeconds, to, from, easingFunction, playDirection);
    }

    public RawTween(object[] targetProperty, float timeInSeconds, T to, object from = null, EasingFunction easingFunction = null, TweenPlayDirection playDirection = TweenPlayDirection.Forward)
    {
      inner = RawTweenImpl(targetProperty, timeInSeconds, to, from, easingFunction, playDirection);
    }

    public RawTween(ValueTuple<object, string> targetProperty, float timeInSeconds, T to, object from = null, EasingFunction easingFunction = null, TweenPlayDirection playDirection = TweenPlayDirection.Forward)
    {
      inner = RawTweenImpl(targetProperty, timeInSeconds, to, from, easingFunction, playDirection);
    }

    // Implementation
    // Create an empty IEnumerator to return from functions that return IEnumerator but don't yield.
    private static IEnumerator YieldBreak = new EmptyEnumerator();

    private IEnumerator RawTweenImpl(SetterFunction<T> setterFunction, float timeInSeconds, T to, object from, EasingFunction easingFunction = null, TweenPlayDirection playDirection = TweenPlayDirection.Forward, GetterFunction<T> getterFunction = null)
    {
      // Perform runtime checks of arguments.
      if (setterFunction == null)
      {
        Debug.LogWarning("setterFunction is null. Will not tween.");
        yield break;
      }
      if(from == null || !(from.GetType().Equals(typeof(T)) || from.GetType().Equals(typeof(GetterFunction<T>))))
      {
        string warningString;
        if(from == null)
        {
          warningString = "from is null.";
        }
        else
        {
          warningString = "from is incorrect type (" + from.GetType().Name + ").";
        }
        Debug.LogWarning(warningString + "from must be either a value of type " + typeof(T).Name + " or a getter function of type GetterFunction<" + typeof(T).Name + ">. Will not tween.");
        yield break;
      }
      if((getterFunction != null) && !getterFunction.GetType().Equals(typeof(GetterFunction<T>)))
      {
        Debug.LogWarning("getterFunction was provided, but it is not of type Func<" + typeof(T).Name + ">. Will not tween.");
        yield break;
      }
      // search in lerpMethodDic for a matching Lerp function.
      Delegate del;
      lerpMethodDic.TryGetValue(typeof(T), out del);
      LerpFunction<T> lerpMethod = del as LerpFunction<T>;
      if (lerpMethod == null)
      {
        // Check if type has a static Lerp function and create a Delegate for it if it has.
        MethodInfo lerpMethodInfo = typeof(T).GetMethod("Lerp", BindingFlags.Static | BindingFlags.Public);
        if (lerpMethodInfo != null)
        {
          lerpMethod = Delegate.CreateDelegate(typeof(LerpFunction<T>), lerpMethodInfo) as LerpFunction<T>;
          Debug.Assert(lerpMethod != null);
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
    
      yield return 0;  // after this yeld, the first iteration follows
    ResetRelative:
      T fromVal;
      T originalVal;
      if(from.GetType().Equals(typeof(T)))
      {
        fromVal = (T)from;
      }
      else if (from.GetType().Equals(typeof(GetterFunction<T>)))
      {
        fromVal = (from as GetterFunction<T>).Invoke();
      }
      else
      {
        fromVal = default(T);
        originalVal = default(T);
        Debug.Assert(false, "from is of invalid type");
      }
      if(getterFunction != null)
      {
        originalVal = getterFunction.Invoke();
      } else
      {
        originalVal = fromVal;
      }
    
    Reset:
      state = RawTweenState.LeftEnd;
      float tweenTime = 0;
      float t = 0;
      if(resetDirection == TweenPlayDirection.Reverse) {
        state = RawTweenState.RightEnd;
        tweenTime = timeInSeconds;
        t = 1.0f;
      }

      while (true)
      {
        tweenTime += (currentPlayDirection == TweenPlayDirection.Forward) ? Time.deltaTime : -Time.deltaTime;
        tweenTime = Mathf.Clamp(tweenTime, 0, timeInSeconds);
        t = tweenTime / timeInSeconds;
        if(state == RawTweenState.Running ||
           (state == RawTweenState.LeftEnd && t > 0.0f) ||
           (state == RawTweenState.RightEnd && t < 1.0f))
        {
          float tweenValue = easingFunction(t);
          T newValue = lerpMethod(fromVal, to, tweenValue);
          setterFunction(newValue);
          state = RawTweenState.Running;
        }
        if(t >= 1.0f)
        {
          state = RawTweenState.RightEnd;
        }
        else if(t <= 0.0f)
        {
          state = RawTweenState.LeftEnd;
          setterFunction(originalVal);
        }
        yield return 0;
        if(state == RawTweenState.Resetting)
        {
          goto Reset;
        }
      }      
    }

    private IEnumerator RawTweenImpl(object[] targetProperty, float timeInSeconds, T to, object from = null, EasingFunction easingFunction = null, TweenPlayDirection playDirection = TweenPlayDirection.Forward)
    {
      if(targetProperty.Length != 2)
      {
        Debug.LogWarning("When passing an an object array as targetProperty, the array is expected to have exactly two elements.");
        return YieldBreak;
      }
      // targetProperty expected to be an obnect with first item the target object and second item the targetPropertyName.
      object target = targetProperty[0];
      if(target == null || !target.GetType().IsValueType)
      {
        Debug.LogWarning("When passing an object array as targetProperty, the first item must be a reference type and not null.");
        return YieldBreak;
      }
      string propertyName = targetProperty[1] as string;
      if(string.IsNullOrEmpty(propertyName))
      {
        Debug.LogWarning("When passing an an object array as targetProperty, the second item must be a string containing the property name.");
        return YieldBreak;
      }
      return RawTweenImpl((target, propertyName), timeInSeconds, to, from, easingFunction, playDirection);
    }

    // Tween a property on some object.
    private IEnumerator RawTweenImpl<U>(ValueTuple<U, string> targetProperty, float timeInSeconds, T to, object from = null, EasingFunction easingFunction = null, TweenPlayDirection playDirection = TweenPlayDirection.Forward) where U : class
    {
      U targetObject = targetProperty.Item1;
      string propertyName = targetProperty.Item2;
      // Perform runtime checks whether the property can be tweened.
      if(targetObject == null)
      {
         Debug.LogWarning("target is null. Will not tween.");
         return YieldBreak;
      }
      if (!targetObject.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Any(p => p.Name == propertyName))
      {
        Debug.LogWarning("Object " + targetObject.ToString() + " does not contain a property named " + propertyName + ". Will not tween.");
        return YieldBreak;
      }
      PropertyInfo propInfo = targetObject.GetType().GetProperty(propertyName);
      Type propType = propInfo.PropertyType;
      if (!propType.Equals(typeof(T)))
      {
        Debug.LogWarning("Property " + propertyName + " on object " + targetObject.ToString() + " is of type " + propType.FullName + " but 'to' is of type " + to.GetType().FullName + ". Will not tween.");
        return YieldBreak;
      }
      SetterFunction<T> setterFunction = (T x) => { propInfo.SetValue(targetObject, x); };
      GetterFunction<T> getterFunction = () => { return (T)propInfo.GetValue(targetObject); };
      if(from == null)
      {
        from = getterFunction;
      }
      return RawTweenImpl(setterFunction, timeInSeconds, to, from, easingFunction, playDirection, getterFunction);
    }
  }
#endregion

#region Easing
  public class Easing
  {
  /*
  The MIT License
  Copyright(c) 2010-2012 Tween.js authors.
  Easing equations Copyright (c) 2001 Robert Penner http://robertpenner.com/easing/
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
    public class Linear
    {
      public static float In(float k)
      {
        return k;
      }

      public static float Out(float k)
      {
        return k;
      }

      public static float InOut(float k)
      {
        return k;
      }
    }

    public class Quadratic
    {
      public static float In(float k)
      {
        return k * k;
      }

      public static float Out(float k)
      {
        return k * (2f - k);
      }

      public static float InOut(float k)
      {
        if ((k *= 2f) < 1f) return 0.5f * k * k;
        return -0.5f * ((k -= 1f) * (k - 2f) - 1f);
      }

      /* 
      * Quadratic.Bezier(k,0) behaves like Quadratic.In(k)
      * Quadratic.Bezier(k,1) behaves like Quadratic.Out(k)
      *
      * If you want to learn more check Alan Wolfe's post about it http://www.demofox.org/bezquad1d.html
      */
      public static float Bezier(float k, float c)
      {
        return c * 2 * k * (1 - k) + k * k;
      }
    };

    public class Cubic
    {
      public static float In(float k)
      {
        return k * k * k;
      }

      public static float Out(float k)
      {
        return 1f + ((k -= 1f) * k * k);
      }

      public static float InOut(float k)
      {
        if ((k *= 2f) < 1f) return 0.5f * k * k * k;
        return 0.5f * ((k -= 2f) * k * k + 2f);
      }
    };

    public class Quartic
    {
      public static float In(float k)
      {
        return k * k * k * k;
      }

      public static float Out(float k)
      {
        return 1f - ((k -= 1f) * k * k * k);
      }

      public static float InOut(float k)
      {
        if ((k *= 2f) < 1f) return 0.5f * k * k * k * k;
        return -0.5f * ((k -= 2f) * k * k * k - 2f);
      }
    };

    public class Quintic
    {
      public static float In(float k)
      {
        return k * k * k * k * k;
      }

      public static float Out(float k)
      {
        return 1f + ((k -= 1f) * k * k * k * k);
      }

      public static float InOut(float k)
      {
        if ((k *= 2f) < 1f) return 0.5f * k * k * k * k * k;
        return 0.5f * ((k -= 2f) * k * k * k * k + 2f);
      }
    };

    public class Sinusoidal
    {
      public static float In(float k)
      {
        return 1f - Mathf.Cos(k * Mathf.PI / 2f);
      }

      public static float Out(float k)
      {
        return Mathf.Sin(k * Mathf.PI / 2f);
      }

      public static float InOut(float k)
      {
        return 0.5f * (1f - Mathf.Cos(Mathf.PI * k));
      }
    };

    public class Exponential
    {
      public static float In(float k)
      {
        return k == 0f ? 0f : Mathf.Pow(1024f, k - 1f);
      }

      public static float Out(float k)
      {
        return k == 1f ? 1f : 1f - Mathf.Pow(2f, -10f * k);
      }

      public static float InOut(float k)
      {
        if (k == 0f) return 0f;
        if (k == 1f) return 1f;
        if ((k *= 2f) < 1f) return 0.5f * Mathf.Pow(1024f, k - 1f);
        return 0.5f * (-Mathf.Pow(2f, -10f * (k - 1f)) + 2f);
      }
    };

    public class Circular
    {
      public static float In(float k)
      {
        return 1f - Mathf.Sqrt(1f - k * k);
      }

      public static float Out(float k)
      {
        return Mathf.Sqrt(1f - ((k -= 1f) * k));
      }

      public static float InOut(float k)
      {
        if ((k *= 2f) < 1f) return -0.5f * (Mathf.Sqrt(1f - k * k) - 1);
        return 0.5f * (Mathf.Sqrt(1f - (k -= 2f) * k) + 1f);
      }
    };

    public class Elastic
    {
      public static float In(float k)
      {
        if (k == 0) return 0;
        if (k == 1) return 1;
        return -Mathf.Pow(2f, 10f * (k -= 1f)) * Mathf.Sin((k - 0.1f) * (2f * Mathf.PI) / 0.4f);
      }

      public static float Out(float k)
      {
        if (k == 0) return 0;
        if (k == 1) return 1;
        return Mathf.Pow(2f, -10f * k) * Mathf.Sin((k - 0.1f) * (2f * Mathf.PI) / 0.4f) + 1f;
      }

      public static float InOut(float k)
      {
        if ((k *= 2f) < 1f) return -0.5f * Mathf.Pow(2f, 10f * (k -= 1f)) * Mathf.Sin((k - 0.1f) * (2f * Mathf.PI) / 0.4f);
        return Mathf.Pow(2f, -10f * (k -= 1f)) * Mathf.Sin((k - 0.1f) * (2f * Mathf.PI) / 0.4f) * 0.5f + 1f;
      }
    };

    public class Back
    {
      static float s = 1.70158f;
      static float s2 = 2.5949095f;

      public static float In(float k)
      {
        return k * k * ((s + 1f) * k - s);
      }

      public static float Out(float k)
      {
        return (k -= 1f) * k * ((s + 1f) * k + s) + 1f;
      }

      public static float InOut(float k)
      {
        if ((k *= 2f) < 1f) return 0.5f * (k * k * ((s2 + 1f) * k - s2));
        return 0.5f * ((k -= 2f) * k * ((s2 + 1f) * k + s2) + 2f);
      }
    };

    public class Bounce
    {
      public static float In(float k)
      {
        return 1f - Out(1f - k);
      }

      public static float Out(float k)
      {
        if (k < (1f / 2.75f))
        {
          return 7.5625f * k * k;
        }
        else if (k < (2f / 2.75f))
        {
          return 7.5625f * (k -= (1.5f / 2.75f)) * k + 0.75f;
        }
        else if (k < (2.5f / 2.75f))
        {
          return 7.5625f * (k -= (2.25f / 2.75f)) * k + 0.9375f;
        }
        else
        {
          return 7.5625f * (k -= (2.625f / 2.75f)) * k + 0.984375f;
        }
      }

      public static float InOut(float k)
      {
        if (k < 0.5f) return In(k * 2f) * 0.5f;
        return Out(k * 2f - 1f) * 0.5f + 0.5f;
      }
    };
  }
#endregion
}