import type Images from "./Images.ts";

export default interface FilmData {
    id: string;
    title: string;
    images: Images;
    supertype: string;
    types: string[];
}
// Här bestämmer vi vad för data som ska hämtas
