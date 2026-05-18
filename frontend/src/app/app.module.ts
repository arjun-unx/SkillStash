import { HttpClientModule } from '@angular/common/http';
import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';

import { AppComponent } from './app.component';
import { AppRoutingModule } from './app-routing.module';
import { CoreModule } from '@core/core.module';

@NgModule({
  declarations: [AppComponent],
  imports: [BrowserModule, HttpClientModule, CoreModule.forRoot(), AppRoutingModule],
  bootstrap: [AppComponent]
})
export class AppModule {}
