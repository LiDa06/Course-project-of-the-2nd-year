using UnityEngine;
using TMPro;

public class VerificationCodeInput : MonoBehaviour
{
    [SerializeField] private TMP_InputField[] inputs;

    void Start()
    {
        for (int i = 0; i < inputs.Length; i++)
        {
            int index = i;
            inputs[i].onValueChanged.AddListener(value =>
                OnValueChanged(index, value));
        }

        inputs[0].ActivateInputField();
    }

    void OnValueChanged(int index, string value)
    {
        if (value.Length == 1)
        {
            if (index < inputs.Length - 1)
            {
                inputs[index + 1].ActivateInputField();
            }
        }
        else if (value.Length == 0 && index > 0)
        {
            inputs[index - 1].ActivateInputField();
        }
    }

    public string GetCode()
    {
        string code = "";
        foreach (var input in inputs)
            code += input.text;

        return code;
    }

    public bool IsCodeLengthCorrect()
    {
        return GetCode().Length == inputs.Length;
    }
}
