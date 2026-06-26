using TMPro;
using UnityEngine;

public class PlayerHand : MonoBehaviour
{
    public TMP_Text TotalCount;

    int _number;

    public void SetNumber(int init)
    {
        _number = init;

        UpdateUI();
    }

    public void AddNumber(int adder)
    {
        _number += adder;

        UpdateUI();
    }

    private void UpdateUI()
    {
        TotalCount.text = $"{_number}/21";
    }
}
