using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public abstract class PlayerAble : MonoBehaviour
{
    //username
    [Header("username")] public Text userNameText;
    //hand area
    [Header("handZone")] public GameObject handZone;
    //drop area
    [Header("dropZone")] public GameObject dropZone;
    //message area
    [Header("message")] public Text messageText;

    private bool _turn = false;
    private bool _firstTurn = false;
    private int _index;
    private string _username;

    public abstract List<GameObject> SetCard(List<GameObject> cardList);
    public abstract void RefreshCard(List<GameObject> cardList);
    public abstract List<GameObject> GetHandCard();
    public bool GetFirstTurn() => _firstTurn;
    public void SetFirstTurn() => _firstTurn = true;
    public void DropFirstTurn() => _firstTurn = false;
    public bool GetTurn() => _turn;
    public void SetTurn() => _turn = true;
    public void DropTurn() => _turn = false;
    public void SetIndex(int index) => _index = index;
    public int GetIndex() => _index;

    public void Win()
    {
        GameManager.Win(_username);
    }

    public void SetUsername(string username)
    {
        _username = username;
        userNameText.text = username;
    }

    public void ClearHandZone()
    {
        foreach (Transform o in handZone.transform)
        {
            Destroy(o.gameObject);
        }
    }

    public void SetHandZone(List<GameObject> list)
    {
        foreach (var o in list)
        {
            o.transform.SetParent(handZone.transform);
        }
    }

    public void Pass()
    {
        //是自己的回合才能pass(It is your own turn to pass)
        if (_turn)
        {
            GameManager.Pass(_index);
            ShowMessage("PASS");
        }
    }

    public void PutCardToDropZone(List<GameObject> playList)
    {
        foreach (var card in playList)
        {
            card.transform.SetParent(dropZone.transform);
            card.GetComponent<PokerController>().enabled = false;
        }
    }

    public void CleanDropZone()
    {
        for (var i = 0; i < dropZone.transform.childCount; i++)
        {
            Destroy(dropZone.transform.GetChild(i).gameObject);
        }
    }

    public void ShowMessage(string message)
    {
        messageText.text = message;
        messageText.gameObject.SetActive(true);
        Invoke("DisableMessage", 3f);
    }

    public void DisableMessage()
    {
        messageText.text = "";
        messageText.gameObject.SetActive(false);
    }
}