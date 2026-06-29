using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

[RequireComponent(typeof(Image))]
public class SudokuCell : MonoBehaviour, IPointerClickHandler
{
    private Image background;
    private TextMeshProUGUI numberText;

    public int Row { get; private set; }
    public int Col { get; private set; }
    public int CurrentValue { get; private set; }
    public bool IsGiven { get; private set; }

    private SudokuGame gameManager;
    private Color textColor;

    public void Init(int row, int col, SudokuGame manager)
    {
        Row = row;
        Col = col;
        gameManager = manager;

        background = GetComponent<Image>();
        if (background == null)
            background = gameObject.AddComponent<Image>();

        numberText = GetComponentInChildren<TextMeshProUGUI>();

        background.raycastTarget = true;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (gameManager != null)
            gameManager.OnCellSelected(Row, Col);
    }

    public void SetValue(int value, bool given)
    {
        CurrentValue = value;
        IsGiven = given;

        if (numberText)
        {
            numberText.text = value > 0 ? value.ToString() : "";
            numberText.fontStyle = given ? FontStyles.Bold : FontStyles.Normal;
        }
    }

    public void SetColors(Color bg, Color text)
    {
        textColor = text;
        if (background) background.color = bg;
        if (numberText) numberText.color = text;
    }

    public void SetBackground(Color color)
    {
        if (background) background.color = color;
    }

    public void FlashError(Color errorColor, Color returnColor)
    {
        StopAllCoroutines();
        StartCoroutine(FlashRoutine(errorColor, returnColor));
    }

    IEnumerator FlashRoutine(Color errColor, Color retColor)
    {
        if (background) background.color = errColor;
        if (numberText) numberText.color = Color.white;
        yield return new WaitForSeconds(0.15f);

        if (background) background.color = retColor;
        if (numberText) numberText.color = textColor;
        yield return new WaitForSeconds(0.1f);

        if (background) background.color = errColor;
        if (numberText) numberText.color = Color.white;
        yield return new WaitForSeconds(0.15f);

        if (background) background.color = retColor;
        if (numberText) numberText.color = textColor;
    }
}