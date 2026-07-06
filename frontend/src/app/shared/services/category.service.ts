import {Injectable} from '@angular/core';
import {ApiService} from '@dsdevq-common/core';
import {type Observable} from 'rxjs';

import {type CategoryModel} from '../models/category/category.model';

/** HTTP-only access to the canonical category reference list (`GET /categories`). */
@Injectable({providedIn: 'root'})
export class CategoryService extends ApiService {
  constructor() {
    super('categories');
  }

  public getCategories(): Observable<CategoryModel[]> {
    return this.get<CategoryModel[]>('');
  }
}
