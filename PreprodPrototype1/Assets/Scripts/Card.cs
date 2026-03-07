using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Card" , menuName = "Card")]
public class Card : ScriptableObject
{
    public enum CardType { Attack, Defend }

    public string cardName;
    public Sprite artwork; // card art
    public CardType type;
    public int manaCost = 1;
    public int effectValue; 

}
