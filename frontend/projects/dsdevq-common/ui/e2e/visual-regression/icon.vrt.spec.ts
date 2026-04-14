import {expect, test} from '@playwright/test';

const STORIES = ['default', 'small', 'large', 'with-color', 'with-aria-label', 'all-sizes', 'common-icons'];

for (const story of STORIES) {
  test(`icon/${story} — light`, async ({page}) => {
    await page.goto(`/iframe.html?id=components-icon--${story}&viewMode=story`);
    await page.waitForSelector('cmn-icon', {timeout: 10000}).catch(() => null);
    await expect(page).toHaveScreenshot(`icon-${story}-light.png`);
  });

  test(`icon/${story} — dark`, async ({page}) => {
    await page.goto(`/iframe.html?id=components-icon--${story}&viewMode=story`);
    await page.evaluate(() => {
      document.documentElement.setAttribute('data-theme', 'dark');
    });
    await page.waitForSelector('cmn-icon', {timeout: 10000}).catch(() => null);
    await expect(page).toHaveScreenshot(`icon-${story}-dark.png`);
  });
}
