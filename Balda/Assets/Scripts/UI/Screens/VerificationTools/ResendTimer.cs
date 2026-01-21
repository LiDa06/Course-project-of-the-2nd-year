using UnityEngine;
using TMPro;
using System.Collections;

namespace Assets.Scripts.UI.Screens.VerificationTools
{
    public class ResendTimer : MonoBehaviour
    {
        [SerializeField] private float seconds = 60f;
        [SerializeField] private TMP_Text timerText;

        public bool IsFinished { get; private set; }

        Coroutine timerCoroutine;

        public void StartTimer()
        {
            if (timerCoroutine != null)
                StopCoroutine(timerCoroutine);

            timerCoroutine = StartCoroutine(TimerRoutine());
        }

        IEnumerator TimerRoutine()
        {
            IsFinished = false;
            float timeLeft = seconds;

            while (timeLeft > 0)
            {
                timerText.text = $"Повторить через {Mathf.CeilToInt(timeLeft)} сек";
                timeLeft -= Time.deltaTime;
                yield return null;
            }

            IsFinished = true;
            timerText.text = "Отправить ещё раз";
        }
    }
}
