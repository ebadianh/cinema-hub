/*
import { useEffect, useState } from "react"
import { useParams } from "react-router-dom"
import type PokemonCardData from "../interfaces/PokemonCardData"
*/
export default function CardInfo() {
/*
    const { id } = useParams()
    const [pokemonCards, setPokemonCards] = useState<PokemonCardData[]>([])
    const [card, setCard] = useState<PokemonCardData>()

    async function getPokemonCardsData() {
        const response = await fetch("/pokemonCards.json")
        const result = await response.json()

        if(response.ok){
        setPokemonCards(result)
        } else {
        alert("Something went wrong!")
        }
    }

    function findCardById(cardId: string) {
        setCard(pokemonCards.find(card => card.id === cardId))
    
    }

    useEffect(() => {
        getPokemonCardsData()
        }, []);

    useEffect(() => {
        findCardById(String(id))
    }, [pokemonCards])

    return <>
        <h2>{card?.name}</h2>
        <img src={card?.images.small} alt="No image" />
    </>
    */
}