import type { JSX } from "react";

export default interface Routes {
  element: JSX.Element;
  path: string;
  menuLabel?: string;
}