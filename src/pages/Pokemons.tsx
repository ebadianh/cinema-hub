/*
import { useEffect, useState } from "react";
import type PokemonCardData from "../interfaces/PokemonCardData.ts";

export default function getAllPokemons() {
  const [pokemonCards, setPokemonCards] = useState<PokemonCardData[]>([]);

  const [searchQuery, setSearchQuery] = useState<string>("");
  const [sortOrder, setSortOrder] = useState<string>("A-Z");

  const [cardCategories, setCardCategories] = useState<string[]>([]);
  const [selectedCaregory, setSelectedCategory] = useState<string>("All");

  const [cardTypes, setCardTypes] = useState<string[]>([])
  const [selectedType, setSelectedType] = useState<string>("All");

  const [currentPage, setCurrentPage] = useState<number>(1);
  const items_per_page = 24;

  async function getPokemonCardsData() {
    const response = await fetch("/pokemonCards.json");
    const result = await response.json();

    if (response.ok) {
      setPokemonCards(result);
    } else {
      alert("Something went wrong!");
    }
  }

  function getAllCardCategories() {
    setCardCategories(["All",
      ...new Set(pokemonCards.map(card => card.supertype))]);
  }
  function getAllCardTypes() {
    setCardTypes(["All",
      ...new Set(pokemonCards.flatMap(card => card.types))]);
  }


  useEffect(() => {
    getPokemonCardsData();
  }, []);

  useEffect(() => {
    getAllCardCategories();
  }, [pokemonCards]);
  
  useEffect(() => {
    getAllCardTypes();
  }, [pokemonCards]);

  useEffect(() => {
    setCurrentPage(1);
  }, [searchQuery,selectedCaregory, selectedType])

  const filteredCards = pokemonCards
    .filter(card => card.supertype === selectedCaregory || selectedCaregory === "All")
    .filter(card => card.types?.includes(selectedType) || selectedType === "All")
    .filter(card => card.name.toLowerCase().includes(searchQuery.toLowerCase()))
    .sort((a, b) => {
      if (sortOrder === "A-Z") {
        return a.name.localeCompare(b.name);
      } else {
        return b.name.localeCompare(a.name);
      }
  });

  const totalPages = Math.ceil(filteredCards.length / items_per_page)
  const startIndex = (currentPage - 1) * items_per_page;
  const paginatedCards = filteredCards.slice(startIndex, startIndex + items_per_page);
  
  return <>
    <input type="text"
    placeholder="Search Pokémon..."
    value={searchQuery}
    onChange={element => setSearchQuery(element.target.value)}
    />
    <select onChange={element => setSelectedCategory(element.target.value)}>
      {cardCategories.map((category, index) => (
        <option key={index} value={category}>{category}</option>
      ))}
    </select>
    <select onChange={element => setSelectedType(element.target.value)}>
        {cardTypes.map((category, index) => (
          <option key={index} value={category}>{category}</option>
        ))}
    </select>
    <select onChange={(element) => setSortOrder(element.target.value)}>
      <option value="A-Z">Namn A-Z</option>
      <option value="Z-A">Namn Z-A</option>
    </select>
    <div className="card-grid">
      {paginatedCards.map((card)=> {
        return <section key={card.id}>
        <h1>{card.name}</h1>
            <a href={"/card/" + card.id}>
              <img src={card.images.small} alt="No image"/>
            </a>
          </section>;
      })}
    </div>
    <div className="pagination">
    <button onClick={() => setCurrentPage(currentPage-1)} disabled={currentPage===1}>
      Föregående
    </button>
    <span>
      Sida {currentPage} av {totalPages}
    </span>
    <button onClick={() => setCurrentPage(currentPage + 1)} disabled={currentPage === totalPages || currentPage > totalPages}>
      Nästa
    </button>
    </div>
  </>;
}
*/