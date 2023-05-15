using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

[Serializable]
public class PokerController : MonoBehaviour, IComparable<PokerController>
{
    public Poker poker;
    public bool select;

    public PokerController(int point, Color color)
    {
        this.poker.point = point;
        this.poker.color = color;
    }

    [Serializable]
    public class Poker : IComparable<Poker>
    {
        [Range(1, 15)] public int point;

        public Color color;

        public int CompareTo(Poker other)
        {
            return point.CompareTo(other);
        }
    }

    public enum Color
    {
        Spade = 4,  //Spade
        Heart = 3,   //Heart
        Club = 2,   //Club
        Diamonds = 1 //Diamonds
    }

    public enum CardType
    {
        单张 = 1, //Single
        对子 = 2, //Double
        干炒 = 3, //Three
        蛇 = 4, //Four
        同花 = 5, //Flush
        葫芦 = 6, //Full House
        金刚 = 7, //Four of a Kind
        同花顺 = 8 //Straight Flush
    }

    private void Start()
    {
        var button = gameObject.AddComponent<Button>();
        button.onClick.AddListener(onClick);
    }

    public void onClick()
    {
        select = !select;
        if (select)
            transform.position += Vector3.up * 25;
        else
        {
            transform.position += Vector3.down * 25;
        }
    }

    public int CompareTo(PokerController other)
    {
        if (poker.point > other.poker.point)
            return 1;
        if (poker.point.Equals(other.poker.point) && poker.color > other.poker.color)
            return 1;
        return -1;
    }
}