using UnityEngine;

public class BlendTreeLooper : StateMachineBehaviour
{
	[Tooltip("Name of the Float parameter that controls the BlendTree")]
	public string blendParam = "Index";

	[Tooltip("How many animations are inside the BlendTree")]
	public int animationCount = 6;

	[Tooltip("How long each animation should play before switching (in seconds)")]
	public float animationDuration = 2f;

	[Tooltip("How long the transition between two animations should take (in seconds, 0 = instant)")]
	[Range(0f, 10f)]
	public float transitionDuration = 1f;

	private float timer;

	private float currentValue;

	private float targetValue;

	private bool isTransitioning;

	private float transitionTimer;

	public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		timer = 0f;
		transitionTimer = 0f;
		isTransitioning = false;
		currentValue = 0f;
		targetValue = 0f;
		animator.SetFloat(blendParam, currentValue);
	}

	public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		timer += Time.deltaTime;
		if (!isTransitioning && timer >= animationDuration)
		{
			timer = 0f;
			transitionTimer = 0f;
			isTransitioning = true;
			targetValue = currentValue + 1f;
		}
		if (isTransitioning)
		{
			transitionTimer += Time.deltaTime;
			float num = ((transitionDuration > 0f) ? (transitionTimer / transitionDuration) : 1f);
			float value = Mathf.Repeat(Mathf.Lerp(currentValue, targetValue, num), animationCount);
			animator.SetFloat(blendParam, value);
			if (num >= 1f)
			{
				isTransitioning = false;
				currentValue = targetValue;
			}
		}
		else
		{
			float value2 = Mathf.Repeat(currentValue, animationCount);
			animator.SetFloat(blendParam, value2);
		}
	}
}
