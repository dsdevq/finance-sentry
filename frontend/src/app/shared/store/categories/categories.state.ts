import {type CategoryModel} from '../../models/category/category.model';

export interface CategoriesState {
  categories: CategoryModel[];
  loaded: boolean;
}

export const initialCategoriesState: CategoriesState = {
  categories: [],
  loaded: false,
};
